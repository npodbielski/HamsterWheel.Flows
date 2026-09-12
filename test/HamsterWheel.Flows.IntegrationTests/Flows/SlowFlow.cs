using HamsterWheel.Flows;
using HamsterWheel.Flows.Blocks.Utils;
using HamsterWheel.Flows.IO;
using HamsterWheel.Flows.Pipelines;

namespace HamsterWheel.Flows.IntegrationTests.Flows;

/// <summary>
/// Runs longer than the pending-result endpoint tests' timeout budget
/// </summary>
public sealed class SlowFlow : NoInputFlow<string>
{
    public override string Name => nameof(SlowFlow);
    public override string Definition => "Finishes after a delay (pending result tests)";
    public override string BuildTypedOutput() => "slow-output";

    protected override Task ApplyToPipelineImpl(IPipeline pipeline, IFlowGlobalInputBag inputs)
    {
        var delay = new DelayBlock();
        pipeline.AddBlock(delay);
        delay.Inputs.Const = 2;
        return Task.CompletedTask;
    }
}
