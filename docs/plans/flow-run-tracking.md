# Plan: Flow run tracking (202 + run status), replacing the platform's 201 + link

## Goal

Unify "the flow run did not reach a terminal result within the request budget" on **one
semantics**: `202 Accepted` + a **queryable run status**. The platform's `201 Created` +
run-link flavor and the 55×1s polling handler are retired in favor of the package's
`IFlowRunStarter` + `FlowRunOutcome.TimedOut`.

Today the package's `202` carries a `runId` **nothing can consume** — there is no status
endpoint and the run's late result is dropped when nobody awaits the completion signal.
That is the missing piece this plan builds.

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

## Milestones / MRs

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
