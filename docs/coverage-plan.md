# Code Coverage Plan — HamsterWheel.Flows

**Goal:** Pass 80% line-coverage gate (`ci/coverage-gate.sh`).

**Current:** 8.32% (38 tests).  
**Strategy:** Test logic-heavy code, exclude trivial wrappers/interfaces via `ExcludeFromCodeCoverage` or coverlet settings. Refactor static IO calls into injectable wrappers where blocks need them.

---

## Conventions

| Rule | Detail |
|------|--------|
| Mocking | `NSubstitute` for all interface mocks |
| Assertions | `FluentAssertions` (`.Should().Be/Contain/Throw<>()` etc.) |
| Structure | `//arrange`, `//act`, `//assert` comments in every test |
| IO | No direct `Directory`, `File`, `Path`, `Process` in testable logic — use thin wrappers (`IFileOps`, `IDirectoryOps`, `IProcessRunner`) |
| Exclusions | Thin wrappers themselves, DI extensions, `IServiceProvider` glue, pure interfaces, records, enums, exception ctors → `[ExcludeFromCodeCoverage]` or coverlet `<Exclude>` |

---

## Phase 0 — Exclusions (no new tests needed)

Add to `Directory.Build.props` or per-file:

```xml
<!-- Coverlet: skip Abstractions project entirely (all interfaces/enums/records) -->
```

In `test/HamsterWheel.Flows.Tests.csproj` or coverlet config:
- **Exclude** `HamsterWheel.Flows.Abstractions.*` — all interfaces, `MapOperation` enum, `FlowName` record, `LogLevel`/`RunStatus` enums, `AssemblyInfo`
- **Exclude** (mark with `[ExcludeFromCodeCoverage]`):
  - `FlowServiceCollectionExtensions` (DI registration)
  - `ModuleInstaller` (DI registration)
  - `INeedServices` (interface)
  - All exception classes that only hold a message: `BlockException`, `FlowException`, `DataException`, `InvalidOsCommandException`, `OsCommandException`, `CouldNotCreateTypeException`, `IncorrectMapOperationValueException`, `SetOutputBlockUsedOnNullOutputException`, `FlowCreationOptionsFlowNameInvalidException`, `FlowNotFoundException`
  - `CronTriggerBase` (abstract, just holds properties)
  - Thin IO wrappers (new, see Phase 1)

**Estimated gain:** ~15-20% of total lines excluded (Abstractions is ~35% of files).

---

## Phase 1 — IO Wrapper Refactoring

Introduce minimal wrappers so blocks are testable without touching the real filesystem:

```csharp
// src/HamsterWheel.Flows/IO/FileOps.cs
public interface IDirectoryOps
{
    IEnumerable<string> GetFiles(string path, string pattern);
    void CreateDirectory(string path);
    void Delete(string path, bool recursive);
    IEnumerable<string> EnumerateDirectories(string path);
    bool Exists(string path);
}

public interface IFileOps
{
    void Copy(string source, string dest, bool overwrite);
}

public interface IPathOps
{
    string Combine(params string[] parts);
    string GetFileName(string path);
    string GetFullPath(string path);
    bool Exists(string path);
}

public interface IProcessRunner
{
    Task<int> Run(string fileName, string arguments, string workingDir,
                  Action<string> onOutput, Action<string> onError,
                  int timeoutSeconds, CancellationToken token);
}
```

Provide concrete implementations (`FileSystemDirectoryOps`, `FileSystemFileOps`, `ProcessRunner`) marked `[ExcludeFromCodeCoverage]`.

**Blocks to refactor:**
- `CopyFilesBlock` → inject `IDirectoryOps` + `IFileOps`
- `MakeDirBlock` → inject `IDirectoryOps`
- `DeleteDirBlock` → inject `IDirectoryOps` + `IPathOps`
- `OsCommandBlock` → inject `IProcessRunner`
- `ProcessExtensions` → can stay but mark excluded (operates on `Process` directly, hard to mock)

---

## Phase 2 — Core Infrastructure Tests

| Class | What to test |
|-------|-------------|
| `Pipeline` | `Run()` happy path, timeout (FlowCancelledException), exception aggregation, `AddBlock` dedup, `AttachCoordinator` |
| `FlowCoordinator` | `AttachPipeline`, `Authorize` (with/without user, anonymous), `SetSuccess` |
| `FlowRunner` | `RunAsync` full flow with mocked factory/applier/pipeline |
| `FlowFactory` | `Create` valid, `Create` with invalid name throws, `Exists` |
| `PipelineFactory` | `Create` wires pipeline + coordinator correctly |
| `FlowApplier` | `Apply` happy path, null input throws, type conversion, validation |
| `FlowContext` | `GetChildContext`, `SetFlow`, property access |
| `FlowGlobalInputBag` | `FromContext` wires services |
| `BlockFactory` | `Create<T>` returns instance, sets name/description |
| `PipelineLogger` | `Log` collects messages |
| `FlowLogMessage` | Constructor/properties |
| `StringCronExtensions` | Valid/invalid/null cron expressions |
| `BasicJsonSerializer` | Serialize/deserialize round-trip |
| `DataValidatorFactory` | Returns validator for registered types |
| `BlockExtensions` | `InitBlock`, `TriggerAfter` chaining |
| `TaskSource` / `TaskSourceExtensions` | `ToBlockingEnumerable`, async enumeration |
| `BlockResult<T>` | `PushOutput`, `Finish`, enumeration |
| `ComplexInputTaskSource` | Merging multiple input sources |
| `SingleInputTaskSource` | Single value passthrough |
| `LinkedBlockEnumerator` | Iterates linked blocks |
| `GlobalInputTransformer` | Transforms input with logger |
| `SimpleRenderingService` | Renders template with variables |
| `FixedUserService` | `Id`, `Name` passthrough |
| `UserPermissionsService` | Permission lookup |
| `FlowBase` / `NoInputFlow` / `NoInputNoOutputFlow` / `NoOutputFlow` | `ApplyToPipeline` adds expected blocks |
| `FlowCreationOptions` | `Validate` throws on empty name |
| `PipelineCreationOptions` | Properties |
| `ScheduledFlowData` | `New`, `Validate` |
| `FlowRunResult` | Constructor |

**Estimated tests:** ~60-80 tests.

---

## Phase 3 — Block Tests

### Base classes (test via concrete subclasses or test doubles)

| Class | What to test |
|-------|-------------|
| `BlockBase.Run()` | Cancellation before/after init, condition false → skip, condition true → run, trigger exception, run exception → `Completion` faulted |
| `PipelineBlock.RunImpl` | Iterates inputs, pushes outputs, finishes result |
| `GroupingBlock.RunImpl` | Collects all inputs, calls `RunForInput(IEnumerable)`, pushes array |
| `MultiplierPipelineBlock` | For each input, calls `RunForInput` with `pushOutput` callback |
| `NoOutPipelineBlock` | Runs, no output |
| `NoInNoOutPipelineBlock` | Runs with no input |

### Concrete blocks

| Block | Test focus |
|-------|-----------|
| `IterateBlock` | Iterates collection, pushes each item |
| `TakeFirstBlock` | Takes first N outputs |
| `TakeAllBlock` | Takes all outputs |
| `LogBlock` | Logs message to pipeline logger |
| `RenderTemplateBlock` | Renders template using `IRenderingService` |
| `CreateObjectBlock` | Creates object via reflection, throws `CouldNotCreateTypeException` |
| `JoinStringsBlock` | Joins strings with separator |
| `MapBlock` | Maps property by name, throws on invalid operation |
| `SetOutputBlock` | Sets output value, throws on null output |
| `SetPropertyBlock` | Sets property on object |
| `SubFlowBlock` | Runs sub-flow via `IFlowFactory` |
| `CopyFilesBlock` | Copies files (mock `IDirectoryOps`/`IFileOps`), recursive |
| `MakeDirBlock` | Creates directory (mock) |
| `DeleteDirBlock` | Deletes if exists, no-op if not (mock) |
| `OsCommandBlock` | Runs process, retries, timeout, non-zero exit (mock `IProcessRunner`) |

**Estimated tests:** ~50-70 tests.

---

## Phase 4 — Scheduler & Triggers

| Class | What to test |
|-------|-------------|
| `FlowScheduler` | `Schedule` with valid/invalid cron, `Cancel`, `CancelAll`, `LoadTriggers` idempotent |
| `CronTriggerBase` | (excluded) |

**Estimated tests:** ~10-15 tests.

---

## Phase 5 — Integration-ish Tests (still unit, just wider scope)

- Full pipeline with 3-4 blocks chained: verify data flows through
- Pipeline timeout: block that never completes → `FlowCancelledException`
- SubFlow: parent pipeline triggers child pipeline
- `FlowRunner.RunAsync` end-to-end with mocked pipeline returning output

**Estimated tests:** ~10-15 tests.

---

## Coverage Estimation

| Phase | Lines covered (est.) | Cumulative % |
|-------|---------------------|--------------|
| Exclusions | -20% of denominator | baseline shifts to ~10% |
| Phase 2 (infrastructure) | +25% | ~35% |
| Phase 3 (blocks) | +30% | ~65% |
| Phase 4 (scheduler) | +5% | ~70% |
| Phase 5 (integration) | +10% | ~80% ✓ |

---

## Execution Order

1. **Phase 0** — Add exclusions, re-run pipeline, get new baseline
2. **Phase 1** — Refactor IO wrappers, update blocks, ensure existing tests still pass
3. **Phase 2** — Infrastructure tests (biggest bang per test)
4. **Phase 3** — Block tests (most volume, but each is small)
5. **Phase 4** — Scheduler tests
6. **Phase 5** — Integration tests to close the gap

Each phase should be its own commit/PR if desired, but all fit on the `fix/alpha-ci` branch.

---

## File: Coverlet Exclusion Config

Add to `src/HamsterWheel.Flows/Directory.Build.props`:

```xml
<ItemGroup>
  <Compile Remove="Setup/**" />
</ItemGroup>
```

Or in the test csproj:
```xml
<PropertyGroup>
  <CoverletExclude>
    HamsterWheel.Flows.Abstractions.*;
    HamsterWheel.Flows.Setup.*;
    HamsterWheel.Flows.ExtensionMethods.FlowServiceCollectionExtensions*
  </CoverletExclude>
</PropertyGroup>
```
