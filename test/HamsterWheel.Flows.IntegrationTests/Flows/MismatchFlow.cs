using HamsterWheel.Flows;
using HamsterWheel.Flows.Blocks.Utils;
using HamsterWheel.Flows.Pipelines;

namespace HamsterWheel.Flows.IntegrationTests.Flows;

public sealed class MismatchFlow : NoInputFlow<MismatchFlowOutput>
{
    public override string Name => nameof(MismatchFlow);
    public override string Definition => "Produces output of a different type than GreetFlow (typed endpoint test)";
    public override MismatchFlowOutput BuildTypedOutput() => new();

    protected override Task ApplyToPipelineImpl(IPipeline pipeline, IFlowGlobalInputBag inputs)
    {
        var setOutput = new SetOutputBlock();
        pipeline.AddBlock(setOutput);
        setOutput.Inputs.PropertyPath.Const = nameof(MismatchFlowOutput.Value);
        setOutput.Inputs.Value.Const = "mismatch-value";

        return Task.CompletedTask;
    }
}
