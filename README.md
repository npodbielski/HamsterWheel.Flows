![Latest Release](https://internetexception.com/wp-content/uploads/flows-badges/release.svg) ![Pipeline](https://internetexception.com/wp-content/uploads/flows-badges/pipeline.svg) ![Tests](https://internetexception.com/wp-content/uploads/flows-badges/tests.svg) ![Unit](https://internetexception.com/wp-content/uploads/flows-badges/coverage-unit.svg) ![Integration](https://internetexception.com/wp-content/uploads/flows-badges/coverage-integration.svg)

# Introduction

HamsterWheel.Flows is a flow and block pipeline runtime for .NET, part of the
[HamsterWheel](https://internetexception.com/why-hamster-wheel/) platform. It lets
you define workflows ("flows") as graphs of data processing units ("blocks") and
run them on demand, at a point in time, or on a cron schedule.

## Reference links

- [Hamster Wheel](https://internetexception.com/why-hamster-wheel/)

# What's contained in this project

This project consists of two main parts:
- main package: `HamsterWheel.Flows` - the main implementation: flows, blocks, pipelines, scheduling and the runner
- `HamsterWheel.Flows.Abstractions` package that can be used to further develop extensions for Flows and adding functionalities missing from main package (custom blocks, flows, input sources)

Navigation:
 - [How to use](#how-to-use)
   - [Setting up the module](#setting-up-the-module)
   - [Defining a flow](#defining-a-flow)
   - [Running a flow](#running-a-flow)
   - [Scheduling flows](#scheduling-flows)
     - [Cron triggers](#cron-triggers)
     - [Background service](#background-service)
 - [Extensions](#extensions)
   - [Custom blocks](#custom-blocks)
   - [Global input transformations](#global-input-transformations)
   - [Extra input bag](#extra-input-bag)
   - [Flow logging](#flow-logging)
 - [Roadmap](#roadmap)

# How to use

Below you can find instructions how to get started using HamsterWheel.Flows in your application.

## Setting up the module

First, reference the main package:
```xml
<PackageReference Include="HamsterWheel.Flows" Version="0.9.0" />
```

After that register the module:
```csharp
services.AddFlowsWithDependencies();
```

`AddFlowsWithDependencies` registers the flows services together with the HLinq
converter stack the runner depends on. If you manage the HLinq services yourself,
use `AddFlowsModule` instead - it registers the flows services only.

Every flow you want to make available is registered separately:
```csharp
services.AddFlow<HelloWorldFlow>();
```

## Defining a flow

A flow is a class that builds a pipeline out of blocks. For flows without input
derive from `NoInputFlow<TOutput>`:
```csharp
public class HelloWorldFlow : NoInputFlow<HelloWorldFlowOutput>
{
    public override string Name => nameof(HelloWorldFlow);
    public override string Definition => "Joins a greeting and sets it on the flow output";
    public override HelloWorldFlowOutput BuildTypedOutput() => new();

    protected override Task ApplyToPipelineImpl(IPipeline pipeline, IFlowGlobalInputBag inputs)
    {
        var join = new JoinStringsBlock();
        pipeline.AddBlock(join);
        join.Inputs.Delimiter.Const = " ";
        join.Inputs.First.Const = "hello";
        join.Inputs.Second.Const = "world";

        var setOutput = new SetOutputBlock();
        pipeline.AddBlock(setOutput);
        setOutput.Inputs.PropertyPath.Const = nameof(HelloWorldFlowOutput.Greeting);
        setOutput.Inputs.Value.SetSource(join, (string s) => (object?)s);
        return Task.CompletedTask;
    }
}

public record HelloWorldFlowOutput
{
    public string? Greeting { get; set; }
}
```

Blocks are wired together through task sources: `Const` sets a fixed value,
`SetSource` connects another block's output (with an optional conversion). The
flow output object is created by `BuildTypedOutput` and populated by the blocks
(e.g. `SetOutputBlock` above). For flows with input derive from
`NoOutputFlow<TInput>` or `FlowBase<TInput, TOutput>` and read the input through
the typed input resolver passed to `ApplyToPipelineImpl`.

## Running a flow

Flows are run through `IFlowRunner` (scoped service):
```csharp
await using var scope = services.CreateAsyncScope();
var runner = scope.ServiceProvider.GetRequiredService<IFlowRunner>();

var message = ScheduledFlowData.New(FlowName.FromString(nameof(HelloWorldFlow)), "user-1", input: null);
var result = await runner.RunAsync(message, cancellationToken);

result.Output; // HelloWorldFlowOutput { Greeting = "hello world" }
```

## Scheduling flows

### Cron triggers

Register any number of cron triggers - each one schedules its flow to run on the
cron expression:
```csharp
public class GreetCronTrigger : CronTriggerBase
{
    public override string Cron => "*/5 * * * * *"; // every 5 seconds (6-field form)
    public override FlowName FlowName => new(nameof(HelloWorldFlow));
}

services.AddSingleton<ICronTrigger, GreetCronTrigger>();
```

Both standard 5-field and 6-field (with seconds) cron expressions are accepted.
One-off scheduling is also available directly through
`IFlowScheduler.Schedule(flowName, userId, at)`.

### Background service

Scheduled runs are written to an internal channel which is consumed by
`FlowBackgroundService`. Register it as a hosted service to actually execute
scheduled flows:
```csharp
services.AddHostedService<FlowBackgroundService>();
```

`FlowBackgroundService` exposes virtual `HandleFlowCompleted` / `HandleFlowError`
methods - derive from it and override them to observe flow runs.

# Extensions

## Custom blocks

A block is a unit of work with typed inputs and outputs. Derive from
`PipelineBlock<TInput, TOutput, TInputSource>`:
```csharp
public class ShoutBlock : PipelineBlock<string, string, SingleInputTaskSource<string>>
{
    public override SingleInputTaskSource<string> Inputs { get; } = new();

    public override Task<string> RunForInput(string input, CancellationToken token)
        => Task.FromResult(input.ToUpperInvariant());
}
```

Blocks are added to the pipeline in `ApplyToPipelineImpl` and wired together
exactly like the built-in ones.

## Global input transformations

To transform block inputs globally (for every flow), derive from
`GlobalInputTransformer` and register a transformation for a block type:
```csharp
public class MyTransformer : GlobalInputTransformer
{
    public MyTransformer()
    {
        RegisterForBlock<JoinStringsBlock, JoinStringsTaskSource, JoinStringsInput>((logger, input) =>
        {
            input?.Delimiter = " ";
            return input;
        });
    }
}
```

## Extra input bag

Hosts can inject extra values available to all flows by implementing
`IExtraInputBag` and registering it - the bag is merged into the flow's global
input bag:
```csharp
public class MyExtraInputBag : IExtraInputBag
{
    public IDictionary<string, object> Bag { get; } = new Dictionary<string, object>
    {
        ["environment"] = "production"
    };
}
```

## Flow logging

Flow and block log messages are emitted through `IPipelineLogger`. The default
`PipelineLogger` writes them to the console - route them elsewhere with
`SetSink`:
```csharp
var logger = provider.GetRequiredService<IPipelineLogger>();
logger.SetSink(message => myStorage.Store(message));
```

# Roadmap
- [ ] Store cron triggers and user schedules in a database (currently in memory)
- [ ] Typo tolerance in input template rendering (Levenshtein distance fallback in the rendering service)
- [ ] Reuse the `SetPropertyBlock` mapping mechanism in `MapBlock`
- [ ] A common way to mark platform exceptions across all HamsterWheel libraries
