# Plan: HamsterWheel.Flows.Api project

## Goal

Create a **`HamsterWheel.Flows.Api` package** in this repository that generalizes
the "run a flow and return the output" endpoint code currently living in the
HamsterWheel platform project (`src/HamsterWheel.Flows.Api` there). Any host —
including the platform — should be able to expose flow-run endpoints without
platform-specific coupling.

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
| success, but output is not `TOutput` (typed overload) | `500` (mismatched output type — expected and actual type, no new exception type needed: the check lives in the endpoint mapping) |
| unknown flow (`FlowNotFoundException`) | `404` |
| run failed | `500` (with error details) |
| run timed out | `202 Accepted` (with run identity — the platform's `201 Created` + run link stays as a platform-specific flavor) |

- input: optional JSON body (the `JsonElement?` binding pattern from the
  integration tests);
- user id: `options.GetUserId` (the platform passes its `ICurrentUserService`
  extraction; default `null`);
- the platform's dynamic-API endpoint classes become thin wrappers: their
  `GetDelegate()` resolves the package's `IFlowRunStarter` and calls the same
  mapping — the platform framework integration stays platform-side by design.

## What stays in the platform project

- the `IMessageExchange`-based `IFlowRunStarter` implementation (message send,
  progress subscription, polling, `MismatchedFlowOutputTypeException` guard),
- the dynamic API endpoint override classes (`IFlowOverrideForEndpoint` etc.),
- `FlowRunProgress` record + messaging worker,
- FluentValidation input validation.

## Implementation steps (small commits, alpha-first MRs)

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

## Decisions

- **No messaging in Flows** — the flow channel (multi-writer, single reader)
  is the mechanism; the `HamsterWheel.Flows.Messaging` records stay in the
  platform.
- **Target framework: `net10.0` only.**

## Open questions

None.
