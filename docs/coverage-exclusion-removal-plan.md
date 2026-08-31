# Plan: Remove ExcludeFromCodeCoverage from Library Code

## Goal

Remove `[ExcludeFromCodeCoverage]` from all library code except thin wrappers and DI glue.
Replace with proper unit tests so coverage is measured honestly.

## Keep Excluded

These are acceptable to remain excluded:

| File | Reason |
|------|--------|
| `Services/IO/DefaultFileSystem` | Thin wrapper around `System.IO` statics |
| `Setup/ModuleInstaller` | Pure DI registration |
| `ExtensionMethods/FlowServiceCollectionExtensions` | Pure DI registration |

## Remove Exclusion + Add Tests

### 1. Exception classes (~11 files)

**Strategy:** Each exception is a record/class with a message. Test construction and message content.

| Exception | Test |
|-----------|------|
| `CouldNotCreateTypeException` | Construct with type name, assert message |
| `IncorrectMapOperationValueException` | Construct, assert message |
| `SetOutputBlockUsedOnNullOutputException` | Construct, assert message |
| `InvalidOsCommandException<T>` | Construct with command, assert message |
| `OsCommandException` | Construct with command, assert message |
| `BlockException` | Construct with block id + message, assert properties |
| `FlowException` | Construct, assert message |
| `FlowCreationOptionsFlowNameInvalidException` | Construct, assert message |

Each test is 3-5 lines. Group into a single `ExceptionTests.cs` file.

### 2. StringCronExtensions (~7 lines)

**Strategy:** Direct unit test.

```
"0 0 * * *" → true
"garbage" → false
null → false
```

### 3. CronTriggerBase (~6 lines)

**Strategy:** Create a test subclass, assert `UserId` is settable.

```csharp
private class TestCronTrigger : CronTriggerBase
{
    public override string Cron => "0 0 * * *";
    public override FlowName FlowName => new("test");
}

[Fact] new TestCronTrigger { UserId = guid } → UserId matches
```

### 4. SubFlowTaskSource (~38 lines)

**Strategy:** Same pattern as `JoinStringsTaskSource`/`CopyFilesInputTaskSource` tests.

```
AllConst → Get() yields single SubFlowBlockInput with correct values
AllSingle → true
Multi-input (FlowName from IAsyncEnumerable) → Get() yields per-item
```

### 5. FlowApplier (~75 lines)

**Strategy:** Mock `IFlowCoordinator`, `IDefaultConverter`, use real `FlowGlobalInputBag` (with mocked `IRenderingService`/`ISerializer`), mock `IFlow` + `IPipeline`.

Tests:
- Happy path: `HaveInput=true`, matching type → `ApplyToPipeline` called, context attached
- `HaveInput=false` → input is null, no converter call
- Type mismatch → converter called, converted value passed
- Converter throws → exception propagated
- `input is IDataWithValidator` → `Validate()` called
- Null input with `HaveInput=true` → throws `FlowNoInputException`
- `coordinator.Authorize` called with correct flow

### 6. FlowScheduler (~99 lines)

**Strategy:** Mock `IServiceProvider` (configure `CreateAsyncScope` → scope with `GetServices<ICronTrigger>`). Use real `Channel<IScheduledFlowData>`.

Tests:
- `Schedule` with valid cron → after timer fires, channel receives `IScheduledFlowData`
- `Schedule` with invalid cron → no exception, no channel write
- `Schedule` with `DateTimeOffset` → timer fires at correct time
- `Cancel(flowName)` → other flows' timers still active, cancelled one stops
- `CancelAll` → all timers stopped
- `LoadTriggers` → picks up all `ICronTrigger` from DI, schedules them
- `LoadTriggers` second call → no-op (TriggersLoaded flag)

**Notes:**
- Use 100-200ms delays for timer-based tests
- `Timer` uses `System.Timers.Timer` which fires on thread pool — use `TaskCompletionSource` or `channel.Reader.WaitToReadAsync` with timeout

### 7. FlowBackgroundService (~114 lines)

**Strategy:** Subclass it (methods are `protected`). Use real `Channel<IScheduledFlowData>`, mock `IFlowScheduler`, real `IServiceProvider` with `IFlowRunner` registered.

```csharp
private class TestFlowBackgroundService(...) : FlowBackgroundService
{
    // override HandleFlowStarted/Completed/Error to record calls
}
```

Tests:
- Write to channel → `ExecuteAsync` picks it up → `IFlowRunner.RunAsync` called
- `IFlowRunner.RunAsync` throws → `HandleFlowError` called, `FailedToScheduleFlow` logged
- Cancellation: cancel token → `ExecuteAsync` returns after running flows complete
- Multiple flows: write 2 to channel → both run
- `ScheduleFlowDirect` → pushes to stack (verify via channel → run)

**Notes:**
- `ExecuteAsync` is an infinite loop — MUST use `CancellationTokenSource` with timeout
- `ProcessFlows` has a 100ms `Task.Delay` polling loop — test must account for this
- `RunFlow` creates an `AsyncServiceScope` — need `IServiceProvider` that supports `CreateAsyncScope()` (use real `ServiceCollection.BuildServiceProvider()`)

### Omitted (covered by other tests)

| Class | Reason |
|-------|--------|
| `FlowRunResult` | Constructed and asserted in `SubFlowBlockTests`, `FlowRunner` tests, and `FlowBackgroundService` tests. Will be naturally covered once those tests exist. |

## Implementation Order

| Step | Classes | Lines | Effort |
|------|---------|-------|--------|
| 1 | Exceptions (11 files) + `StringCronExtensions` + `CronTriggerBase` | ~30 | Trivial |
| 2 | `SubFlowTaskSource` | ~38 | Low |
| 3 | `FlowApplier` | ~75 | Medium |
| 4 | `FlowScheduler` | ~99 | Medium |
| 5 | `FlowBackgroundService` | ~114 | Medium-High |

Each step: remove attributes → write tests → verify coverage gate still passes → push.

## Estimated Coverage Impact

- Currently: **80.04%** with ~350 lines excluded (incl. exceptions)
- After removing exclusions + adding tests: expected **80-83%**
- Exception tests are trivial and will cover 100% of those lines
- Runner tests will cover ~90% of previously-excluded lines
- `FlowRunResult` gets covered incidentally by runner tests
- Net effect: positive on the 80% gate

## Risks

| Risk | Mitigation |
|------|-----------|
| `FlowScheduler` timer tests are flaky | Generous timeouts (2s), use channel `WaitToReadAsync` |
| `FlowBackgroundService.ExecuteAsync` infinite loop | Always CTS with 5s timeout, assert on captured events |
| `FlowApplier` is `internal` | `InternalsVisibleTo` already configured for test project |
| Coverage drops below 80% during transition | Implement in order (easy first), push after each step |
