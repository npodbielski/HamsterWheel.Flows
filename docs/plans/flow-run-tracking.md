# Plan: Flow run tracking — the run resource behind `201 + Location`

## Goal

Build the **run resource** the platform's `201 Created + Location` promises. The status code
describes the *request*; the progress of the flow is a property of the *run resource*, so a run that
outlives the request budget is answered with a pointer that must actually be dereferenceable.
Today nothing serves that pointer.

Decided response contract (implemented in the package as of 0.8.0 — `docs/plans/flows-api-project.md`,
second pass item 1):

| Endpoint | run finished within budget | run still going after the budget |
|---|---|---|
| `POST /install/flow` | `200 OK` + output | `202 Accepted` |
| `POST /{extension}/{entity}` (flow-overridden create) | `200 OK` + output | `201 Created` + `Location: /core/flows/{flowName}/run/{runId}` |

Rationale: the created entity is the **FlowRun**, not the flow's business outcome (`POST /orders` —
the order exists even if delivery later fails). First install is the exception, and it is `202`
precisely because at that moment no run resource can exist yet (see below).

The package supports both flavors already: `FlowRunResults.Pending(outcome, urlTemplate)` answers
`201 + Location` when a run location can be produced, falling back to `202` when the starter
reported no run id. **What is missing is the resource itself** — the tracker + status endpoint
below, which is what turns the platform's `201` from a promise into an answer.

## The run resource exists — but only after install, and only after the flush

The resource behind the link is real: the source generator produces a Core-extension single handler
for `FlowRunEntity` at `flows/runs/{id:guid}` (plus `flows/runs/logs/{id:guid}` for the run log), so
`GET /core/flows/runs/{runId}` returns `{id, flow{id, name, link}, status, exception, input, …}`.
Two caveats decide what still has to be built here:

- **it is a Core extension endpoint, so it answers only once Core is installed** — during first
  install the flow itself creates that extension, which is why `POST /install/flow` is `202` and not
  `201 + Location`;
- **it is eventually consistent** — `SystemLogStorageService` buffers `IFlowRunMessage` in memory and
  writes to the Core `IFlowLogStorage` only once it reports `GetIsReady()` (errors are swallowed and
  the message re-queued), so a `GET` right after `201 + Location` can miss the row. Persisting the
  run before responding is the open question at the bottom of this plan.

The `201` flavor therefore needs no new route for the platform — it needs the ordering fixed, and
`CoreExtensionFlowRunPath` (`flows/runs`) has to be reused instead of the nested template (below).
The other finding from the live response: the run resource **serializes the whole flow input**
(names, e-mail, `createToken`, provider settings). Anything that surfaces run input — including the
proposed install progress endpoint — has to redact, see
`../platform/docs/plans/install-run-progress-endpoint.md`.

## Current state

### Flows package (0.7.0)

- `ChannelFlowRunStarter` creates `runId` (= `ScheduledFlowData.ScheduledId`), attaches a
  `Completion` signal, awaits it up to the timeout; on timeout returns
  `FlowRunOutcome(TimedOut, RunId)` and **the eventual result of the still-running flow is
  lost** (the TCS completes with no reader).
- `MapFlowRunEndpoint` maps `TimedOut` → `202 Accepted` with `{flowName, runId, message}` —
  a dead end: no `Location`, no way to query the run.

### Platform (`feature/flows-nuget-packages`, merged to platform `alpha`)

- Vendored core replaced by packages (`0.7.0-alpha-13726`); runtime is the package channel
  (`SimpleMessageExchange` writes `IScheduledFlowData` to the channel;
  `PlaltformFlowBackgroundService` [sic] emits progress messages).
- **`HamsterWheel.Flows.Api` is referenced nowhere** (version entry pinned at the stale
  `13522`); no `IFlowRunStarter` usage.
- The endpoint→flow-run chain is still entirely platform code:
  `FirstInstallFlowEndpoints` → `FlowEndpoint.Run` → `FlowEndpointHandler.Start`
  (bus send + `IFlowRunProgressMessage` subscribe + **55×1s poll**) →
  `FlowEndpointCommon.ReturnResult` (200 / **201 + link** to a `FlowRunProgress` resource /
  throw→500 / 409).
- `FlowRunProgress` + the progress messages stay useful (live UI progress) — they just stop
  being the wait mechanism.

## Design

`runId` is the same `Guid` in both worlds (package `ScheduledFlowData.ScheduledId` ==
platform `ScheduleFlowRunMessage.ScheduledId`), so one tracker serves both.

### 1. Flows package — run tracking (new, in `HamsterWheel.Flows.Api`)

**`IFlowRunTracker`** (Abstractions-free, lives in the Api package):

```csharp
public interface IFlowRunTracker
{
    void TrackStarted(Guid runId, IFlowName flowName, DateTimeOffset startedAt);
    void TrackCompleted(Guid runId, object? output);
    void TrackFailed(Guid runId, Exception error);
    bool TryGet(Guid runId, out TrackedFlowRun run);   // null when unknown/expired
}

public record TrackedFlowRun(
    Guid RunId, IFlowName FlowName, FlowRunTrackingStatus Status,   // Running | Completed | Failed
    DateTimeOffset StartedAt, DateTimeOffset? CompletedAt,
    object? Output, Exception? Error);
```

- `InMemoryFlowRunTracker` (singleton): bounded — `TrackRunLimit` (default 1000) +
  `TrackRetention` (default 30 min), oldest evicted first. Thread-safe (runs complete on
  background-service threads).

**Wiring — late completions must be captured even after the endpoint already returned 202:**
`ChannelFlowRunStarter` gets an optional `IFlowRunTracker?` (resolved via DI, null = off).
After writing the channel it calls `TrackStarted`, and attaches a continuation on the
`Completion` TCS that records `TrackCompleted/TrackFailed` **independently of the request** —
the request-side await stays exactly as today. A plain decorator cannot do this (it never
sees the TCS), hence starter-side wiring.

**Status endpoint** in `FlowsApiExtensions`:

```csharp
// GET /api/flows/runs/{runId:guid}
public static IEndpointConventionBuilder MapFlowRunStatusEndpoint(
    this IEndpointRouteBuilder endpoints, MapFlowRunStatusEndpointOptions? options = null);
```

| State | Response |
|---|---|
| Running | `202` `{runId, flowName, status}` |
| Completed | `200` `{runId, flowName, status, completedAt, output}` |
| Failed | `200` `{runId, flowName, status, completedAt, error}` (error message; not 500 — the run itself is a completed fact) |
| Unknown / expired | `404` |

- Output payload serialized like the run endpoint's `200` does; `IncludeOutput` option to
  suppress (large/sensitive outputs).
- Authorization is host-side (standard endpoint `RequireAuthorization()` on the convention
  builder returned).

**202 from the run endpoint becomes actionable:**
`MapFlowRunEndpointOptions.RunStatusUrlTemplate` (default `/api/flows/runs/{runId}`) →
the `202` response gains a **`Location` header** and a `statusUrl` field in the body.

**`AddFlowsApi`** registers `InMemoryFlowRunTracker` + passes it to `ChannelFlowRunStarter`
(tracking on by default; `FlowRunTrackingOptions.Enabled = false` to opt out).

### 2. Platform adoption (`feature/flows-nuget-packages` line)

1. Reference `HamsterWheel.Flows.Api`; `services.AddFlowsApi()`; pin all three packages to
   **one version** (single MSBuild property — also fixes the stale `Flows.Api 0.7.0-alpha-13522`
   pin).
2. `FlowEndpointHandler<TInput,TOutput>` (+ `NoInput*` variants): replace bus-send + subscribe +
   55s poll with `IFlowRunStarter.RunAsync(FlowName.FromString(FlowName), userId, input,
   timeout, token)` — the runtime is already the channel; the endpoint stops caring how the
   run is started. `FlowEndpoint<TInput,TOutput>` declarative classes stay (dynamic API
   framework) — now genuinely thin.
3. `FlowEndpointCommon.ReturnResult` maps `FlowRunOutcome`:
   Success → `200` output · Failed(`FlowNotFoundException`) → `404` · Failed → throw the
   stored exception (platform flavor → 500) · **TimedOut → `202` + `Location`** (replaces
   `201 Created` + `FlowRunProgress` link). No `409` (no such outcome).
   The typed endpoints keep throwing `MismatchedFlowOutputTypeException<TOutput>` (now the
   package type).
4. `MapFlowRunStatusEndpoint()` mapped in the platform's endpoint setup so the `Location`
   resolves; keep emitting `IFlowRunProgressMessage` from `PlatformFlowBackgroundService`
   for UI progress (fix the class-name typo `PlaltformFlowBackgroundService` in the same pass).
5. Update `FirstInstallFlowEndpoints` (`ProducesResponseType<FlowRunProgress>(201)` →
   `202` + status response type) and any client/installer code that treats `201` as the
   pending state (**client-visible breaking change — enumerate callers before switching**).

## 201 vs 202 — how the decision was reached

Both are 2xx “success” codes, so **neither is distinguishable by a client that only checks
`IsSuccessStatusCode`** — that argument is a wash and belongs to the `Location`/body argument
below, not to the status code itself.

| | `201 Created` + run link (platform today) | `202 Accepted` + run id (package today) |
|---|---|---|
| HTTP meaning | “a new resource was created” — claims the resource is ready, while the body says it is still running | “accepted for processing, work not finished” — matches what actually happened (the request stopped waiting) |
| Body | full `FlowRunProgress` snapshot (status, progress %, input, output) — useful with **zero** follow-up requests | small `{flowName, runId, message}`; only as useful as the status endpoint behind it |
| Requirements | needs a routable run resource the host owns (`/{core}/flow-run/{id}`) → host coupling, nothing in the packages provides it | needs `IFlowRunTracker` + `MapFlowRunStatusEndpoint`; the in-memory tracker is per-process and lost on restart (multi-instance → sticky routing / shared store) |
| Clients | already published contract (`StatusResponseTypes {201: IFlowRunProgressMessage}`, `ProducesResponseType<FlowRunProgress>(201)` on the installer) — no break | breaking for installer/UI; generated typed clients must be regenerated |
| Async hygiene | no polling hint; naive clients read it as “done, here is the entity” | composes with `Location` + `Retry-After`; standard async-API shape (ARD-style) that tooling understands |
| Failure/late result | progress message stream is the wait mechanism today (the 55×1s poll) | late completion captured by the tracker even after the request returned |

**Decided:** the code follows **store readiness**, not taste — the run resource exists and is
dereferenceable → `201 Created + Location`; it does not (yet) → `202 Accepted`. Consequences:

- data endpoints keep (and finally honor) `201 + Location`; the run status endpoint below is a
  **prerequisite**, not an alternative — the `201` switch must not ship before it exists;
- first install answers `202`, because its own flow creates the Core extension, the migration and
  the DB, so no run resource can exist while it runs (its current `201 + link` is unreachable
  exactly in the failure case);
- the pending mapping stays host-overridable (`MapFlowRunEndpointOptions.RunUrlTemplate` /
  `TimedOutResult`, `FlowRunResults.Pending`), so a host without a run resource is never forced
  into a broken `201`;
- rejected: always `200` with a `{status, runId, output}` envelope — trivial for typed clients but
  it throws away HTTP-level signalling.

## How a run's log lines get here — `IFlowRunLogObserver`

The tracker knows *that* a run finished. A follow-up screen (the `GET /flow/run/{runId}` status
endpoint, or a host's own progress endpoint) also wants *what the run said while it ran*.

The package offers that as
[`IFlowRunLogObserver`](../../src/HamsterWheel.Flows.Abstractions/Monitoring/IFlowRunLogObserver.cs):
opt-in like `IFlowProgressObserver` (injected into `PipelineFactory`, which gives every pipeline its
own wrapper), and handed the identity of the run that wrote the line.

It deliberately is **not** a sink on `IPipelineLogger.SetSink`. There is one logger per host, so a
sink registered there — the way the platform did it — sees the lines of every pipeline, is called
once per sink *registered so far* (so lines multiply as runs accumulate, each copy carrying whichever
run id stamped it last), and never says who wrote it. A per-run observer is what makes attribution
possible at all, and costs nothing where nobody registers one.

Two rules for whoever consumes it:

- **mask before keeping** — the package hands over the line as written, because it cannot know what
  is secret; the run *input* is never part of it (see [`flow-secrets.md`](flow-secrets.md)).
- **keep a bounded tail, not a history** — the durable copy of a run's logs is storage
  (`FlowRunLog`); an in-memory tail answers "what is it doing right now" and is capped by line count
  and line length. One immutable line per log line: no per-line dictionary, no per-run copy of the
  host's whole log.
- **a sub-flow is not the run** — `FlowRunIdentity.IsSubFlow` says so, because a sub-flow reports
  under its own run id: hosts that attribute lines to the run someone is watching (or to a `FlowRun`
  row) must not merge the two.

## Milestones / MRs

0. **flows → alpha** (done): `IFlowRunLogObserver` — per-run log attribution, replacing the
   accumulating `IPipelineLogger.SetSink` pattern.
1. **flows → alpha**: `IFlowRunTracker` + `InMemoryFlowRunTracker` + starter wiring
   (late-completion capture) + unit tests (success/fail/late-complete-after-timeout/eviction).
2. **flows → alpha**: `MapFlowRunStatusEndpoint` + `Location`/`statusUrl` on `202` +
   integration tests (`202` carries `Location`; status: running→202, done→200, expired→404).
3. **platform**: package bump (single version property), starter-based handlers,
   `201 → 202` switch, status endpoint mapping, client/callers update, tests.

## Risks / breaking changes

- **`201 → 202` changes the platform's public HTTP contract** for long runs — installer/UI
  consumers must be updated in the same platform MR.
- In-memory tracker: run status is **lost on restart** and per-process (multi-instance needs
  sticky routing or a shared store later — out of scope; `IFlowRunTracker` is the seam).
- Tracker memory: outputs are held until retention — `IncludeOutput=false` / `Enabled=false`
  for heavy flows.

## Open questions

- Failed run: `200` with error summary (proposed) vs `500`?
- Retention defaults (1000 runs / 30 min) right for the platform?
- Should the platform's run-history storage (`IFlowLogStorage`) become an `IFlowRunTracker`
  consumer (single source of truth) instead of `FlowRunProgress` messages — later.
- ~~`201 → 202`: adopt, or keep `201 + link` as the host flavor?~~ **Decided:** per-endpoint, by
  store readiness (contract at the top of the plan).
- Should `201`/`202` carry `Retry-After`? (Suggested poll interval for clients that know nothing
  about the status endpoint.)
- `201 + Location` implies the run row exists before the response: persist in the starter before
  responding (honest `201`) or accept eventual consistency and document it? The platform's run
  storage is currently async + best-effort.
- Route shape: the **existing** generated resource is `/core/flows/runs/{runId}`
  (`CoreExtensionFlowRunPath`), while the decided contract named `/core/flows/{flowName}/run/{runId}`.
  The generated route needs no flow name and survives flow renames — recommend pointing `Location`
  at the existing one and dropping the nested template (package side: it is only a URL template, so
  both work).
