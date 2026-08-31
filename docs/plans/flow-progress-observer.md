# Plan: Flow progress observer

## Goal

A **public observer of flow progress** so consumer projects (e.g. the HamsterWheel
platform) can track running flows — started/finished, per-block progress, failing
blocks with their exceptions, and the flow output. This is the prerequisite for a
Web UI of running flows, their results and progress.

## Current state

What exists today and where the data lives:

| Data | Where | Notes |
|---|---|---|
| Block output | `IPipelineBlock<TOutput>.Result` (`BlockResult<TOutput>`) | `SingleValue`, `AsEnumerable()`, `GetLastOutput()`, `IsSingle` — **outputs only** |
| Block failure | `BlockBase.Completion` (faulted `Task`) | faulted with typed `BlockOperationException` / `BlockTriggerException` / `BlockConditionException` (all `BlockException`, carry the block `Id`) |
| Block identity | `IPipelineBlock`: `Id`, `Description`, `Inputs` | |
| Run identity | `Pipeline.Options` (`IPipelineCreationOptions`) | `RunId` (Guid, set from `ScheduledFlowData.ScheduledId`), `UserId` |
| Flow output | `Pipeline.FlowContext.Output` | |
| Run lifecycle | `Pipeline.Run(CancellationToken)` | single `Task.WhenAll(Blocks.Select(b => b.Run(token)))` + a max-run-time `delay` — **no per-block visibility today** |

Note: the block `Result` property does **not** contain exceptions — the exception is
on the block's `Completion` task (public on `BlockBase`, but declared `internal` on
`IPipelineBlock`, so external consumers cannot read it through the interface). The
observer API below therefore passes the exception explicitly instead of making
consumers inspect `Completion`.

## Proposed design

New public surface: **one interface + one small record** in
`HamsterWheel.Flows.Abstractions` (namespace `HamsterWheel.Flows.Monitoring`).
No other new public types.

```csharp
public sealed record FlowRunIdentity(Guid RunId, IFlowName FlowName, string? UserId);

//Consumer projects register an implementation via DI to observe flow activity
//(e.g. a Web UI showing running flows, progress and failing blocks).
//Implementations must be thread-safe - blocks complete concurrently.
public interface IFlowProgressObserver
{
    void FlowStarted(FlowRunIdentity run, int blockCount);
    void BlockCompleted(FlowRunIdentity run, IPipelineBlock block);
    void BlockFailed(FlowRunIdentity run, IPipelineBlock block, Exception exception);
    void FlowCompleted(FlowRunIdentity run, object? output);
    void FlowFailed(FlowRunIdentity run, Exception error);
}
```

`blockCount` from `FlowStarted` gives consumers the progress denominator
(`completed / blockCount`); a block that has not reported yet is "in progress".

### Wiring (internal changes only)

- `Pipeline` constructor: add an **optional** `IFlowProgressObserver? observer = null`
  parameter (optional so the 4 existing construction sites in tests keep working).
- `PipelineFactory`: add `IServiceProvider` to the constructor and resolve
  `GetService<IFlowProgressObserver>()` — `null` when unregistered, so the observer
  is **opt-in** and existing hosts see no behavior change. (Deliberately not
  registered in `AddFlowsModule` — an observer is a consumer concern, not a
  default.)
- `Pipeline.Run` changes:
  - build `FlowRunIdentity` from `Options.RunId`, `Flow` name and `Options.UserId`;
  - notify `FlowStarted(run, Blocks.Count)` after pipeline init;
  - run blocks as an array of `(block, task)` pairs and attach a `ContinueWith`
    per block that fires `BlockCompleted` / `BlockFailed` (with the faulting
    exception) as each block finishes — this is what makes progress *live*
    instead of all-at-once at the end of `Run`;
  - notify `FlowCompleted(run, FlowContext.Output)` on success and
    `FlowFailed(run, error)` on the failure and timeout (`FlowCancelledException`)
    paths, using the exception the existing logic already computes.
  - all observer invocations are wrapped in `try/catch` (a faulting observer must
    never break a flow; the error is written to `IPipelineLogger`).

### Consumer example (platform Web UI)

```csharp
public sealed class FlowProgressStore : IFlowProgressObserver
{
    private readonly ConcurrentDictionary<Guid, FlowRunState> _runs = new();
    //FlowRunState: status, blockCount, completedBlocks, failedBlocks (Id + type + exception), output
    //...
}

services.AddSingleton<IFlowProgressObserver, FlowProgressStore>();
//a Web UI endpoint reads FlowProgressStore; the messaging worker that emits
//IFlowRunProgressMessage can also update FlowRunProgress.Progress from block
//completions (completed / blockCount) instead of coarse status changes
```

## Alternatives considered

1. **Per-block observer property** (the `InputTransformer` pattern, set by
   `BlockFactory`) — rejected: pollutes `IPipelineBlock` and `BlockFactory` for
   something the pipeline can do strictly less invasively.
2. **Events / `Action` delegates on `Pipeline`** — rejected: not DI-friendly, not
   testable, not a clean public API for consumer projects.
3. **Extending `IPipelineLogger`** — rejected: the logger carries text log messages,
   not structured progress; mixing the two concerns would force every logger to
   ignore most events.
4. **Pull model (`IObservableFlow` with awaitable per-block tasks)** — rejected for
   v1: the push observer matches "the platform observes activity" and is simpler;
   the pull model can be added later if needed.

## Implementation steps (small commits, alpha-first MRs)

1. `FlowRunIdentity` + `IFlowProgressObserver` in `HamsterWheel.Flows.Abstractions`.
2. `Pipeline`: optional observer parameter; per-block and flow-level notifications
   in `Run` (with the safe-invocation helper).
3. `PipelineFactory`: resolve the observer via `IServiceProvider`.
4. Unit tests (recording observer):
   - notification ordering (started → blocks → completed),
   - `BlockFailed` carries the block's exception,
   - `FlowCompleted` carries the flow output,
   - a throwing observer does not break the flow,
   - no observer registered = no behavior change.
5. Integration test: host with a capturing observer — cron-triggered
   `HelloWorldFlow`-style run observed end-to-end (extend the existing
   integration fixtures).
6. Pre-push checks per `AGENTS.md` (tests + coverage gate + format), MR to alpha.

## Open questions

- **Per-block "started" event** — blocks start after their dependencies complete and
  `RunImpl` has no pre-hook today; v1 treats "not yet completed" as in-progress.
  Add only if the Web UI needs it.
- **Type-erased per-block output snapshot** — v1: consumers that know their block
  types can read `Result` directly; the flow-level output is passed to
  `FlowCompleted`. Add a non-generic output accessor to `BlockResult` later if the
  UI needs per-block values generically.
- **`RunId` type** — `Guid` (matches `PipelineCreationOptions.RunId` and the
  platform's `FlowRunProgress.ScheduledId`).
