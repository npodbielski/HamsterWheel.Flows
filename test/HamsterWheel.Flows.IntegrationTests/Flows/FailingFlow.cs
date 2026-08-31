using HamsterWheel.Flows;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Base;
using HamsterWheel.Flows.IO;
using HamsterWheel.Flows.Pipelines;

namespace HamsterWheel.Flows.IntegrationTests.Flows;

public sealed class FailingFlow : NoInputFlow<string>
{
    public override string Name => nameof(FailingFlow);
    public override string Definition => "Always fails (integration test)";
    public override string BuildTypedOutput() => "output";

    protected override Task ApplyToPipelineImpl(IPipeline pipeline, IFlowGlobalInputBag inputs)
    {
        var throwing = new ThrowingBlock();
        pipeline.AddBlock(throwing);
        throwing.Inputs.Const = new object();
        return Task.CompletedTask;
    }
}

internal sealed class ThrowingBlock : PipelineBlock<object, string, SingleInputTaskSource<object>>
{
    public override SingleInputTaskSource<object> Inputs { get; } = new();
    public override Task<string> RunForInput(object input, CancellationToken token) =>
        throw new InvalidOperationException("boom");
}
