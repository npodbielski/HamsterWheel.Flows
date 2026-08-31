using HamsterWheel.Flows;
using HamsterWheel.Flows.Blocks.Utils;
using HamsterWheel.Flows.IO;
using HamsterWheel.Flows.Pipelines;

namespace HamsterWheel.Flows.IntegrationTests.Flows;

public sealed class GreetFlow : NoInputFlow<GreetFlowOutput>
{
    public override string Name => nameof(GreetFlow);
    public override string Definition => "Joins a greeting and sets it on the flow output";
    public override GreetFlowOutput BuildTypedOutput() => new();

    protected override Task ApplyToPipelineImpl(IPipeline pipeline, IFlowGlobalInputBag inputs)
    {
        var join = new JoinStringsBlock();
        pipeline.AddBlock(join);
        join.Inputs.Delimiter.Const = " ";
        join.Inputs.First.Const = "hello";
        join.Inputs.Second.Const = "world";

        var setOutput = new SetOutputBlock();
        pipeline.AddBlock(setOutput);
        setOutput.Inputs.PropertyPath.Const = nameof(GreetFlowOutput.Greeting);
        setOutput.Inputs.Value.SetSource(join, (string s) => (object?)s);

        return Task.CompletedTask;
    }
}

public sealed record GreetFlowOutput
{
    public string? Greeting { get; set; }
}
