using HamsterWheel.Flows;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Base;
using HamsterWheel.Flows.Blocks.Utils;
using HamsterWheel.Flows.IO;
using HamsterWheel.Flows.Pipelines;

namespace HamsterWheel.Flows.Tests.Api;

internal sealed record StarterSuccessFlowOutput
{
    public string? Value { get; set; }
}

internal sealed class StarterSuccessFlow : NoInputFlow<StarterSuccessFlowOutput>
{
    public override string Name => nameof(StarterSuccessFlow);
    public override string Definition => "Produces a constant output (starter test)";
    public override StarterSuccessFlowOutput BuildTypedOutput() => new();

    protected override Task ApplyToPipelineImpl(IPipeline pipeline, IFlowGlobalInputBag inputs)
    {
        var join = new JoinStringsBlock();
        pipeline.AddBlock(join);
        join.Inputs.Delimiter.Const = "-";
        join.Inputs.First.Const = "hello";
        join.Inputs.Second.Const = "from-starter";

        var setOutput = new SetOutputBlock();
        pipeline.AddBlock(setOutput);
        setOutput.Inputs.PropertyPath.Const = nameof(StarterSuccessFlowOutput.Value);
        setOutput.Inputs.Value.SetSource(join, (string s) => (object?)s);

        return Task.CompletedTask;
    }
}

internal sealed class StarterFailingFlow : NoInputFlow<string>
{
    public override string Name => nameof(StarterFailingFlow);
    public override string Definition => "Always fails (starter test)";
    public override string BuildTypedOutput() => "output";

    protected override Task ApplyToPipelineImpl(IPipeline pipeline, IFlowGlobalInputBag inputs)
    {
        var throwing = new StarterThrowingBlock();
        pipeline.AddBlock(throwing);
        throwing.Inputs.Const = new object();
        return Task.CompletedTask;
    }
}

internal sealed class StarterThrowingBlock : PipelineBlock<object, string, SingleInputTaskSource<object>>
{
    public override SingleInputTaskSource<object> Inputs { get; } = new();
    public override Task<string> RunForInput(object input, CancellationToken token) =>
        throw new InvalidOperationException("starter test failure");
}

internal sealed class StarterHangingFlow : NoInputFlow<string>
{
    public override string Name => nameof(StarterHangingFlow);
    public override string Definition => "Hangs until the pipeline max run time (starter timeout test)";
    public override string BuildTypedOutput() => "output";

    //max run time = AverageTime * 3 = 300ms, so the run is still in progress at the starter's timeout budget
    public override TimeSpan? AverageTime => TimeSpan.FromMilliseconds(100);

    protected override Task ApplyToPipelineImpl(IPipeline pipeline, IFlowGlobalInputBag inputs)
    {
        var hanging = new StarterHangingBlock();
        pipeline.AddBlock(hanging);
        hanging.Inputs.Const = new object();
        return Task.CompletedTask;
    }
}

internal sealed class StarterHangingBlock : PipelineBlock<object, string, SingleInputTaskSource<object>>
{
    public override SingleInputTaskSource<object> Inputs { get; } = new();

    public override async Task<string> RunForInput(object input, CancellationToken token)
    {
        await Task.Delay(Timeout.Infinite, token);
        return "never";
    }
}
