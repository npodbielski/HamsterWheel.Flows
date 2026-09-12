# Plan: HamsterWheel.Flows.Api project

## Goal

Create a **`HamsterWheel.Flows.Api` package** in this repository that generalizes
the "run a flow and return the output" endpoint code currently living in the
HamsterWheel platform project (`src/HamsterWheel.Flows.Api` there). Any host —
including the platform — should be able to expose flow-run endpoints without
platform-specific coupling.

## Status (0.7.0+)

**Implemented in 0.7.0** (`src/HamsterWheel.Flows.Api`): `IFlowRunStarter` / `FlowRunOutcome` /
`FlowRunStatus`, `ChannelFlowRunStarter`, `AddFlowsApi()`, `MapFlowRunEndpoint` /
`MapFlowRunEndpoint<TOutput>`, `MapFlowRunEndpointOptions`. Sections 1–3 describe the shipped
shape.

**Moved in 0.8.0** (`HamsterWheel.Flows.Api.Endpoints`): `FlowRunEndpointBase` (validation +
the pending decision), the handler contracts and the handler bases the generator generates
against (`FlowRunStarterHandler` and friends) — see [item 4](#4-generator-facing-base-classes-move-here).
The run is started as the user of `IFlowUserService`, not as a host-specific current user, and the
handler maps with the flow's output type (`FlowRunResults.Map<TOutput>` / `OkOutput`).

Still not moved (tracked by the [second generalization pass](#second-generalization-pass)): the
output-type guard is unreachable outside the typed overload, there is no validate-before-run step
of `MapFlowRunEndpoint` (the typed path validates through the endpoint base), and
`FlowEndpoint` + its verb/no-input variants — which are the *metadata* half of the same classes
(`IFlowOverrideForEndpoint`, dependency resolution) — still live in the platform.

The platform does **not** reference the package yet — its `Directory.Packages.props` pins
`HamsterWheel.Flows.Api` at a stale `0.7.0-alpha-13522` while core is at `13726`, and the whole
endpoint → flow-run chain (`FirstInstallFlowEndpoints` → `FlowEndpoint.Run` →
`FlowEndpointHandler.Start` (bus + 55×1s poll) → `FlowEndpointCommon.ReturnResult`) is still
platform code. Consumption is planned in `platform/docs/plans/use-flows-packages.md` (Phase 3).

`docs/plans/flow-run-tracking.md` proposes **202 + queryable run status** in place of the
platform's 201 + link. That is a **proposal, not a decision** (trade-offs are tabulated there); the
package itself has shipped 202 for its own endpoint since 0.7.0, and whichever way it goes, the
`runId` the 202 carries has to become consumable.

## Current state (platform project)

The platform's `HamsterWheel.Flows.Api` contains:

- **Endpoints** (`Endpoints/`): `FlowEndpoint<TInput, TOutput>` (plus
  `ForSingle`/`ForCreate`/`ForUpdate`/`ForDelete`/`ForList`) and the
  `NoInputFlowEndpoint` / `NoOutputFlowEndpoint` / `NoInputNoOutputFlowEndpoint`
  variants. They are declarative classes for the platform's *dynamic API
  framework*: `DataOperation`, `HttpMethod`, `Route`, `ExtensionName`,
  `GetDelegate()`, `Resolve(IServiceProvider)` (resolves
  `IFlowEndpointHandler<TInput>` + optional `IValidator<TInput>`), then
  `Run(input)` → validate → `handler.Start(input)` →
  `FlowEndpointCommon.ReturnResult(...)`.
- **Handlers** (`Handlers/`): `FlowEndpointHandler<TInput, TOutput>` /
  `NoInputFlowEndpointHandler<TOutput>` — the actual run orchestration:
  - build `ScheduleFlowRunMessage(FlowName, userId, input)` (user from
    `ICurrentUserService`),
  - `IMessageExchange.Send(message)` +
    `Subscribe<IFlowRunProgressMessage>(UpdatePipelineInfo)` — the flow runs in a
    messaging worker, not inline,
  - poll up to **55 × 1s** until the run leaves `{Scheduled, InProgress}`,
  - output type guard: `IFlowRunSuccessMessage { Output: not TOutput }` →
    `MismatchedFlowOutputTypeException<TOutput>`,
  - return the final `IFlowRunMessage`.
- **Result mapping** (`FlowEndpointCommon.ReturnResult`):
  - success with output → `200 Ok(output)`,
  - still in progress (timeout) → `201 Created` with a link to the run,
  - failed → **throws** the stored exception (→ 500),
  - otherwise → `409 Conflict`.
- **Coupling to platform infrastructure**: `IMessageExchange` (platform message
  bus), `ICurrentUserService` (platform auth), the dynamic API framework
  (`IFlowOverrideForEndpoint`, `IDependencyResolver`), `HamsterWheel.DI`,
  FluentValidation — and the platform **vendors a copy of this library**
  (`src/HamsterWheel.Flows` there = this codebase + `Messaging` + `Monitoring`
  additions).

## What gets generalized into the package

The platform code has two concerns: *how a flow run is started and tracked*
(messaging — platform-specific) and *how an HTTP endpoint exposes that*
(generalizable). The package abstracts the first and provides the second.

### New project

`src/HamsterWheel.Flows.Api/HamsterWheel.Flows.Api.csproj`:
- `net10.0`, `FrameworkReference Microsoft.AspNetCore.App`,
  `ProjectReference` to `HamsterWheel.Flows`;
- added to `HamsterWheel.Flows.slnx`;
- packaging: the `package-alpha` / `package` jobs pack `src/**`, so the new
  package is picked up automatically — **verify** it appears in the alpha job
  output in the first alpha pipeline.

### 1. Run-starter seam

#### The seam

```csharp
public enum FlowRunStatus
{
    Success,
    Failed,
    TimedOut
}

public sealed record FlowRunOutcome(
    FlowName FlowName,
    FlowRunStatus Status,
    object? Output,
    Exception? Error);

//How a flow run is started and tracked. The package ships a direct implementation;
//hosts with a message bus (e.g. the platform) provide their own.
public interface IFlowRunStarter
{
    Task<FlowRunOutcome> RunAsync(
        FlowName flowName,
        string? userId,
        object? input,
        TimeSpan runTimeout,
        CancellationToken token);
}
```

One method — the platform's four handler variants (`FlowEndpointHandler`,
`NoInputFlowEndpointHandler`, `NoOutputFlowEndpointHandler`,
`NoInputNoOutputFlowEndpointHandler`) differ only in input handling, so they
collapse into the single nullable `input` parameter.

#### Shipped implementation (built-in channel - `ChannelFlowRunStarter`)

The default starter uses the library's designed run path: it writes the run to
the registered flow channel (as one of its writers) and `FlowBackgroundService`
executes it; the starter awaits a per-run completion signal:

```csharp
//registered: services.AddSingleton<IFlowRunStarter, ChannelFlowRunStarter>()
//requires FlowBackgroundService to be registered as a hosted service
public sealed class ChannelFlowRunStarter(Channel<IScheduledFlowData> channel) : IFlowRunStarter
{
    public async Task<FlowRunOutcome> RunAsync(
        FlowName flowName, string? userId, object? input,
        TimeSpan runTimeout, CancellationToken token)
    {
        var completion = new TaskCompletionSource<IFlowRunResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var message = new ScheduledFlowData(Guid.NewGuid(), DateTimeOffset.UtcNow, flowName,
            userId, input, false)
        {
            Completion = completion
        };
        message.Validate();
        channel.Writer.TryWrite(message);

        try
        {
            var result = await completion.Task.WaitAsync(runTimeout, token);
            return new FlowRunOutcome(flowName, FlowRunStatus.Success, result.Output, null);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return new FlowRunOutcome(flowName, FlowRunStatus.TimedOut, null, null);
        }
        catch (Exception e)
        {
            return new FlowRunOutcome(flowName, FlowRunStatus.Failed, null, e);
        }
    }
}
```

Two small core additions make this possible (neither changes existing behavior):
- `ScheduledFlowData` gets an optional in-memory completion signal
  (`TaskCompletionSource<IFlowRunResult>? Completion { get; init; }`) — the
  record is created by the caller, so the signal rides on the message;
- `FlowBackgroundService` completes it after the run: `SetResult(result)` on
  success, `SetException(error)` on failure.

The starter writes the channel directly (not through `IFlowScheduler.Schedule`)
because it must attach the completion signal — exactly the multi-writer
relaxation from section 2. Note that flow resolution now happens in the
background service, so an unknown flow comes back as a `Failed` outcome whose
`Error` is `FlowNotFoundException` (the endpoint maps that to 404) instead of a
thrown exception. Hosts that prefer an inline run (no background service) can
write their own starter on top of `IFlowRunner` (~15 lines).

#### Platform implementation (stays in the platform project)

Same logic as today's `FlowEndpointHandler.Start`, wrapped into the seam — the
55s poll becomes the `runTimeout` parameter:

```csharp
public sealed class MessagingFlowRunStarter(IMessageExchange messageExchange) : IFlowRunStarter
{
    public async Task<FlowRunOutcome> RunAsync(
        FlowName flowName, string? userId, object? input,
        TimeSpan runTimeout, CancellationToken token)
    {
        var message = new ScheduleFlowRunMessage(flowName, userId, input);
        message.Validate();

        IFlowRunMessage progress = message;
        await using var unsub = messageExchange.Subscribe<IFlowRunProgressMessage>(update =>
        {
            if (update.ScheduledId == message.ScheduledId) progress = update;
            return Task.CompletedTask;
        });
        await messageExchange.Send(message);

        var deadline = DateTimeOffset.UtcNow + runTimeout;
        while (DateTimeOffset.UtcNow < deadline
               && (progress is IScheduleFlowRunMessage
                   || progress is IFlowRunProgressMessage { Status: RunStatus.Scheduled or RunStatus.InProgress }))
        {
            await Task.Delay(1000, token);
        }

        return progress switch
        {
            IFlowRunSuccessMessage s => new FlowRunOutcome(flowName, FlowRunStatus.Success, s.Output, null),
            IFlowRunFailedMessage f => new FlowRunOutcome(flowName, FlowRunStatus.Failed, null, f.Exception),
            _ => new FlowRunOutcome(flowName, FlowRunStatus.TimedOut, null, null)
        };
    }
}
```

Note vs the shipped `FlowRunOutcome`: it carries `RunId`, and a messaging starter **must** fill
it (from `message.ScheduledId`) — a `TimedOut` outcome without a run id cannot be turned into a
202 + status link (item 5 of the second pass). The `MismatchedFlowOutputTypeException` guard does
not belong in the starter: the starter does not know `TOutput`.

#### Consumption (the endpoint - identical in both hosts)

```csharp
app.MapFlowRunEndpoint(new MapFlowRunEndpointOptions
{
    GetUserId = ctx => ctx.User?.Id, //platform: its ICurrentUserService extraction
    RunTimeout = TimeSpan.FromSeconds(55)
});
//internally: starter.RunAsync(...) -> 200 Ok(output) /
//404 (Failed with Error of type FlowNotFoundException) / 500 (Failed) /
//202 (TimedOut)
```

#### Semantics

- **`TimedOut` means "no terminal result within the budget" — what happens to
  the run itself is implementation-defined**: in the channel starter the run
  keeps going in the background service (the endpoint just stops waiting), as
  in the messaging starter (which is why the platform returns a "check back
  later" 201/202). The endpoint maps both to 202.
- **`userId` and `runTimeout` are parameters, not starter state** — auth
  extraction and the HTTP timeout budget are endpoint concerns; the starter
  just does the run. That keeps both implementations free of ASP.NET and auth
  coupling.

### 2. Should Flows ship a SimpleMessaging layer?

Considered and **recommended against**:

- The platform's `HamsterWheel.Messaging.Simple` is an in-memory
  `IMessageExchange` — a `BackgroundService` with a message stack and
  type-based pub/sub. It carries no durability or cross-process capability,
  and its own code carries the TODO: *"use System.Channels instead"*.
- Flows already has the right primitive: `Channel<IScheduledFlowData>` —
  purpose-built for flow runs, with `FlowBackgroundService` as the single
  reader.

Instead of adding a messaging abstraction, the plan is:

- **Relax the channel to multiple writers** — `SingleWriter = false` in
  `AddFlowsModule`. (The current `SingleWriter = true` is already technically
  violated: every cron trigger has its own timer writing concurrently.) The
  single reader stays `FlowBackgroundService`.
- **Any host component schedules through the public
  `IFlowScheduler.Schedule(flowName, userId, at / cron)`** — the API endpoint,
  a worker, anything. The background service runs the flow.
- **`IFlowProgressObserver` (Plan 1) provides live progress** for a Web UI —
  in-process, no progress messages needed.

When a real message bus is still required (durability across restarts,
multi-process worker tiers, cross-service messaging), the platform keeps its
`IMessageExchange` and implements `IFlowRunStarter` on top of it — the seam is
exactly what makes that swappable.

### 3. Endpoint mapping (minimal API)

```csharp
public static class FlowsApiExtensions
{
    //POST /api/flows/{flowName}/run - flow resolved by name from AddFlow<T>() registrations
    public static IEndpointConventionBuilder MapFlowRunEndpoint(
        this IEndpointRouteBuilder endpoints, MapFlowRunEndpointOptions? options = null);

    //Same as above, but enforces the flow's output type: a successful run whose
    //output is not TOutput fails with 500 (expected and actual type) instead of
    //returning a mismatched payload - the platform's MismatchedFlowOutputType behavior
    public static IEndpointConventionBuilder MapFlowRunEndpoint<TOutput>(
        this IEndpointRouteBuilder endpoints, MapFlowRunEndpointOptions? options = null);
}

public sealed record MapFlowRunEndpointOptions
{
    public string RouteTemplate { get; init; } = "/api/flows/{flowName}/run";
    public Func<HttpContext, string?>? GetUserId { get; init; } //pluggable auth extraction
    public TimeSpan RunTimeout { get; init; } = TimeSpan.FromSeconds(55);
}
```

Generalized result mapping (from `FlowEndpointCommon`):

| Case | Response |
|---|---|
| success | `200 Ok(output)` |
| success, but output is not `TOutput` (typed overload) | `500` — throws the core `MismatchedFlowOutputTypeException<TOutput>` (expected + actual type) |
| unknown flow (`FlowNotFoundException`) | `404` |
| run failed | `500` (`ProblemDetails` with the error message) |
| run timed out | `202 Accepted` + `{flowName, runId, message}` — no `Location` yet; `flow-run-tracking.md` adds the status endpoint that makes `runId` queryable |

- input: optional JSON body (the `JsonElement?` binding pattern from the
  integration tests);
- user id: `options.GetUserId` (the platform passes its `ICurrentUserService`
  extraction; default `null`);
- the platform's dynamic-API endpoint classes become thin wrappers: their
  `GetDelegate()` resolves the package's `IFlowRunStarter` and calls the same
  mapping — the platform framework integration stays platform-side by design.

## What stays in the platform project

- the `IMessageExchange`-based `IFlowRunStarter` implementation (message send, progress
  subscription, wait-until-timeout). The `MismatchedFlowOutputTypeException` guard is **not**
  part of it — the guard is a package concern (second pass, item 2);
- the dynamic API endpoint *framework*: `IApiEndpointMetadata` / `IFlowOverrideForEndpoint` +
  the verb marker interfaces, `ExtensionName` / `Route` / `DataOperation` / `HttpMethod` /
  `StatusResponseTypes` / `GetDelegate()`, `DynamicEndpointsCollection`, `AddFlowEndpoint` /
  `AddFlowOverriderFor*` registrations, and the `201 + run link` timeout flavor layered on top of
  the package result mapping;
- `FlowRunProgress` record + messaging worker + the `IFlowRun*Message` bus contracts (once
  `flow-run-tracking.md` lands, `FlowRunProgress` should be a projection of the package's
  tracked-run record);
- validator *implementations* (`AddValidatorsFromAssemblyContaining<TDbContext>()`) — but **not**
  the validation step: `HamsterWheel.Flows.Api` references FluentValidation and owns
  validate-before-run (decision below).

## Second generalization pass

Validated against the platform at `alpha` (`7b6a48da`, flows packages `0.7.0-alpha-13726`).
Ordered by dependency; 1–3 and 5–8 are small package-only MRs, 4 is the generator-coupled one.

### 1. Public result mapping — ✅ done (0.8.0)

> Implemented as `FlowRunResults` (`Map`, `Map<TOutput>`, `Accepted`, `Created`, `Pending`,
> `RunLocation`) with `MapFlowRunEndpointOptions.RunUrlTemplate` / `TimedOutResult`. The pending
> case follows the decided contract in `flow-run-tracking.md`: `201 Created + Location` when the
> host has a run resource, `202 Accepted` otherwise. What remains is consumer-side: the platform
> deleting `FlowEndpointCommon` in favor of it, and `FlowRunResults` gaining the 400/validation
> mapping when item 3 lands.

Before this, `FlowsApiExtensions.MapOutcome` was `private`, so the platform had no choice but to keep
`FlowEndpointCommon.ReturnResult`. Extract it as API:

```csharp
public static class FlowRunResults
{
    public static IResult Map(FlowRunOutcome outcome, Func<FlowRunOutcome, IResult>? success = null);
    public static IResult Map<TOutput>(FlowRunOutcome outcome); // output guard + 200
    // hosts override only the timeout case: platform = 201 Created + run link
    public static IResult Map(FlowRunOutcome outcome, Func<FlowRunOutcome, IResult> success,
        Func<FlowRunOutcome, IResult> timedOut);
}
```

`MapFlowRunEndpoint` calls it; declarative endpoints call it and keep only their timeout flavor.
Deletes `FlowEndpointCommon` from the platform.

### 2. Output-type guard as public API

`FlowRunResults.Map<TOutput>` (or `FlowRunOutcome.GuardOutput<TOutput>()`) so the platform can
delete its copy from the progress callback. Today the guard is locked inside the
`MapFlowRunEndpoint<TOutput>` lambda *and* duplicated in `FlowEndpointHandler.UpdatePipelineInfo`
— and it must not move into the starter, which knows nothing about `TOutput`.

### 3. Validate-before-run moves to the package (FluentValidation dependency is fine)

`FlowEndpoint.Run` runs `IValidator<TInput>.ValidateAndThrowAsync` before starting; the package
endpoint has no equivalent, which is the only real reason it is not a full replacement today.
There is no architectural blocker: `FluentValidation` (11.11.0), `FluentValidation.AspNetCore`
and `FluentValidation.DependencyInjectionExtensions` are **already pinned** in
`Directory.Packages.props` and referenced by no flows project — the dependency is simply unused
so far. Add `PackageReference FluentValidation` to `HamsterWheel.Flows.Api` and:

```csharp
public sealed record MapFlowRunEndpointOptions
{
    // resolve the validator for this run (null = no validation);
    // platform: sp.GetService(typeof(IValidator<>).MakeGenericType(inputType))
    public Func<FlowName, object?, IServiceProvider, IValidator?>? GetInputValidator { get; init; }
}
```

Validation runs before `starter.RunAsync`; a failure maps to `400 ValidationProblem` (what the
platform's `ProducesValidationProblem` advertises), never to 500. The typed/generator path
(item 4) resolves `IValidator<TInput>` in `Resolve`, exactly as `FlowEndpoint` does today.

### 4. Generator-facing base classes move here

Flow code generators are planned for this repo, and the classes generated code derives from are
flow-generic, not platform-specific. Move to `HamsterWheel.Flows.Api` under
`HamsterWheel.Flows.Api.Endpoints`:

| Move | Why |
|---|---|
| `FlowEndpoint<TInput, TOutput>` | the run pipeline itself: validate (3) → `IFlowRunStarter.RunAsync` → `FlowRunResults.Map` (1, 2); nothing in it is platform-specific once the metadata is an interface the *host* declares |
| `FlowEndpointFor{Create,Update,Delete,Single,List}<TInput,TOutput>` | each is two constant overrides (`DataOperation`, `HttpMethod`); the verb marker interfaces stay platform, the base classes do not |
| `NoInputFlowEndpoint` / `NoOutputFlowEndpoint` / `NoInputNoOutputFlowEndpoint` | same pipeline minus the body parameter — they exist only because the generator needs a base type per input/output shape |
| `IFlowEndpointHandler<TInput>` / `INoInputFlowEndpointHandler` | the handler contract is the generator's DI vocabulary (`AddScoped<IFlowEndpointHandler<TInput>, …>()`); keep as a compat facade over `IFlowRunStarter` |

Stays platform: `IFlowOverrideForEndpoint` + verb markers, `IApiEndpointMetadata`,
`ExtensionName` / `Route` / `StatusResponseTypes` / `GetDelegate()`, `IDependencyResolver`,
`DynamicEndpointsCollection`, `AddFlowEndpoint` / `AddFlowOverriderFor*`.

**Hard constraint — the generator hardcodes these names.**
`FlowEndpointBaseTypeConfigurator` emits `HamsterWheel.Platform.Flows.Endpoints.FlowEndpoint` /
`FlowEndpointForCreate` / … by string, `HandlerGenerator` emits
`HamsterWheel.Platform.Flows.Handlers.FlowEndpointHandler` with an
`(IMessageExchange, ICurrentUserService)` primary constructor, and `EndpointGenerator` emits a
body calling `Resolve(context.RequestServices); return await Run(...)`. So:

1. move the implementations, then keep the 9 platform classes as empty `abstract` shims deriving
   from the package ones — existing extensions keep compiling, nothing has to regenerate at once;
2. when the flow generators land in this repo (FluentCodeGenerators 0.6.3 and Roslyn are already
   pinned for exactly this), retarget `FlowEndpointBaseTypeConfigurator` / `HandlerGenerator` to
   the package namespace in the same change that flips extension flow JSON; `HandlerGenerator` can
   be dropped entirely — an endpoint + `IFlowRunStarter` needs no per-flow handler class;
3. finally delete the shims (breaking; pre-1.0 minor bump per `AGENTS.md`).

While steps 1–3 are in flight, `Resolve(IServiceProvider)` and the `Run(input, token)` /
`Run(token)` signatures are **frozen** — every generated extension compiles against them.

### 5. `RunId` must be non-nullable and always populated

`FlowRunOutcome.RunId` is `Guid?` and only `ChannelFlowRunStarter` sets it. The timeout response
(and the `flow-run-tracking.md` status endpoint) is meaningless without it. Make it `Guid` and
have every starter populate it (messaging: `ScheduledFlowRunMessage.ScheduledId`).

### 6. `userId` type at the seam

Seam is `string?`; the platform is `Guid GetCurrentUserId()` → `Guid? UserId` on
`ScheduleFlowRunMessage`, while `IScheduledFlowData.UserId` is `string?`. Either keep `string?`
and document the Guid↔string conversion as an adapter responsibility, or add a `Guid?` overload —
but do not leave the platform guessing.

### 7. Flow-run OpenAPI metadata helper

The flow-specific half of `StatusResponseTypes` (200 output / 202 accepted / 404 / 500) as a
`ProducesFlowRunResults()` convention-builder extension; hosts append their extras (the
platform's `{201: IFlowRunProgressMessage}`) on top. Small, but it is the last piece of
`FlowEndpointCommon`'s job.

### 8. `AddFlowsApi()` fills gaps instead of registering over the host (`TryAdd`) — ✅ done (0.8.0)

Today:

```csharp
services.AddSingleton<IFlowRunStarter, ChannelFlowRunStarter>();  // last registration wins → whoever calls last owns the seam
if (!HasFlowBackgroundService(services)) { services.AddHostedService<FlowBackgroundService>(); }
```

`AddSingleton` + a hand-rolled collection scan makes the method **order-sensitive**: a host that
calls `AddFlowsApi()` before registering its own `IFlowRunStarter` gets the channel starter
shadowed (or, worse, the wrong starter last), and a host with a `FlowBackgroundService`
derivative registered afterwards gets **two** hosted readers of the same channel. Make it gap
filling instead:

```csharp
public static IServiceCollection AddFlowsApi(this IServiceCollection services)
{
    //host-registered implementations always win, in either order
    services.TryAddSingleton<IFlowRunStarter, ChannelFlowRunStarter>();
    services.TryAddSingleton<IFlowRunTracker, InMemoryFlowRunTracker>();   // flow-run-tracking.md

    //dedupes the exact type; the assignability scan still needed for host derivatives
    if (!HasFlowBackgroundService(services))
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, FlowBackgroundService>());
    }

    return services;
}
```

Semantics to document on the method: **“call this *after* your own flow/DI registrations — it only
fills the gaps and never replaces what you registered.”**

- `TryAddSingleton<IFlowRunStarter, …>` removes the ordering constraint entirely for the seam that
  matters (a host starter wins whether it registered before or after), makes the method
  idempotent, and lets library glue call it defensively.
- `TryAddEnumerable` dedupes the exact hosted-service type (two `AddFlowsApi()` calls, or a host
  that registered `FlowBackgroundService` itself). **Residual limitation to state in the doc
  comment:** `TryAddEnumerable` compares `(ServiceType, ImplementationType)`, so a host
  *derivative* (`PlaltformFlowBackgroundService`) is a different implementation type — the
  `HasFlowBackgroundService` type-assignability scan has to stay, and it can only see
  registrations made *before* the call. That is exactly why the comment says “call me last”.
  If that heuristic ever becomes a nuisance, the honest fallback is to drop the hosted-service
  half from `AddFlowsApi()` and expose `AddFlowBackgroundService()` explicitly.
- Applies to every default the package gains later (`IFlowRunTracker` from
  `flow-run-tracking.md`, any future `IFlowInputValidator` default): defaults are `TryAdd`, hosts
  own overrides — never `Add` a default over a host's type.
- Platform consequence (Phase 3): with this in place, calling `AddFlowsApi()` after registering
  `MessagingFlowRunStarter` is safe; the only thing still to watch is the hosted-service half.

## Implementation steps (small commits, alpha-first MRs)

### Done (0.7.0)

Steps 1–6 shipped: multi-writer channel + `ScheduledFlowData.Completion`, the project scaffold,
`IFlowRunStarter` + `ChannelFlowRunStarter`, `MapFlowRunEndpoint{,<TOutput>}`, tests, MR to
alpha. Step 7 (platform consumption) has not happened.

### Original steps

1. Library changes (small standalone MR, alpha first): relax the flow channel
   to multiple writers (`SingleWriter = false` in `AddFlowsModule`) so any host
   component can schedule via `IFlowScheduler`; add the
   `ScheduledFlowData.Completion` signal and complete it in
   `FlowBackgroundService` (the `ChannelFlowRunStarter` prerequisite).
2. Scaffold `src/HamsterWheel.Flows.Api` (csproj, slnx entry), verify the
   alpha pipeline publishes the package.
3. `FlowRunStatus` / `FlowRunOutcome` / `IFlowRunStarter` +
   `ChannelFlowRunStarter`.
4. `MapFlowRunEndpoint` + `MapFlowRunEndpoint<TOutput>` (output-type guard) +
   `MapFlowRunEndpointOptions` + result mapping.
5. Unit tests: starter logic against a real channel + background service
   (success, failure, timeout, unknown flow); integration tests: in-process
   `WebApplication` with `ChannelFlowRunStarter` (200 + output, 404, 500, and
   500 on mismatched output via the typed overload — reusing the existing
   integration fixtures, which already register a `FlowBackgroundService`
   derivative).
6. Pre-push checks per `AGENTS.md`, MR to alpha.
7. **(Platform repo, separate follow-up)** reference the package; the platform's
   `HamsterWheel.Flows.Api` shrinks to the messaging starter + dynamic-API glue;
   separately consider replacing the vendored library copy with the published
   package.

### Next (second generalization pass)

8. Items 1, 2, 5, 6 — package-only, no generator coupling, no platform break: public
   `FlowRunResults`, public output guard, non-nullable `RunId`, `userId` type decision. One MR.
   **Item 1 (with item 8) shipped in 0.8.0** — `FlowRunResults` + `RunUrlTemplate`/`TimedOutResult`
   + `TryAdd*` defaults; items 2 (standalone guard API), 5 and 6 remain.
9. Item 3 — FluentValidation validate-before-run (`400 ValidationProblem` mapping + tests).
10. Item 4 — **partly done (0.8.0)**: `FlowRunEndpointBase` + handler contracts + handler bases
    moved (the handler of an endpoint with output is now `IFlowEndpointHandler<TInput, TOutput>` —
    the generator registers/derives with *both* type arguments). Remaining: `FlowEndpoint` + the
    verb/no-input variants, leaving platform shims (API freeze on `Resolve`/`Run`).
11. Item 7 — `ProducesFlowRunResults()` OpenAPI helper.
11b. Item 8 — `AddFlowsApi()` on `TryAdd*` + “call after your registrations” doc comment (pair
    with the `IFlowRunTracker` MR).
12. **(later, this repo)** the flow code generators: retarget the base-type names to the package
    namespace, drop `HandlerGenerator`, then delete the platform shims.

## Decisions

- **Inline-first stays.** `MapFlowRunEndpointOptions.RunTimeout` (default `55s`) is the contract: the
  endpoint waits and answers `200` + output for runs that finish in time, and only the long tail
  leaves with the pending response. Most flows are quick, so one call that returns the result is the
  usable API — callers are never *forced* to poll. The wait needs a limit anyway and ~1 minute is the
  natural one: `55s` sits **under the ubiquitous 60s proxy / idle timeout**, so the inline response
  cannot be cut off mid-request. Hosts behind a shorter proxy lower `RunTimeout`; it stays the single
  source of truth (replacing the handlers' hardcoded `tries < 55`). Run tracking /
  progress endpoints are additive (long runs, UI progress), not a step toward “always 202 + poll”.
- **No messaging in Flows** — the flow channel (multi-writer, single reader)
  is the mechanism; the `HamsterWheel.Flows.Messaging` records stay in the
  platform.
- **Target framework: `net10.0` only.**
- **`HamsterWheel.Flows.Api` may reference FluentValidation.** Nothing forces validate-then-run to
  stay platform-side — the package already owns everything else around the run, so it owns
  validation too; hosts contribute validator instances, not pipeline code.
- **Package defaults are registered with `TryAdd*`.** `AddFlowsApi()` fills gaps — host
  registrations always win — and says so in its doc comment (“call after your own registrations”)
  instead of relying on the host to call it first.
- **Generator-facing classes belong with the generators.** Since the flow generators are moving to
  this repo, the base classes generated code derives from (`FlowEndpoint` and variants, the
  handler contracts) move here as well; the platform keeps the dynamic-API metadata plus temporary
  name-compatibility shims.

## Open questions

- Do the `IFlowRun*Message` bus contracts move to the package (they are the vocabulary of any
  bus-backed `IFlowRunStarter`) or stay platform? Currently: stay.
- `userId`: keep `string?` at the seam, or add a `Guid?` overload (platform is `Guid` land,
  `IScheduledFlowData` is `string` land)?
- Where do the moved generator-facing types land: `HamsterWheel.Flows.Api` (they are API surface)
  or a generator-facing face that does not depend on ASP.NET? Leaning: `HamsterWheel.Flows.Api` —
  they need `IResult` anyway.
- Version pinning policy for `-alpha` flows packages: the platform's `Flows.Api` pin already
  drifted behind core (`13522` vs `13726`).
