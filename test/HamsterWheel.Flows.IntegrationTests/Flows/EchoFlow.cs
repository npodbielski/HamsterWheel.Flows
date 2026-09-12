using HamsterWheel.Flows;
using HamsterWheel.Flows.Blocks.Utils;
using HamsterWheel.Flows.IO;
using HamsterWheel.Flows.Pipelines;

namespace HamsterWheel.Flows.IntegrationTests.Flows;

public sealed record EchoInput
{
    public string? Message { get; init; }
}

public sealed record EchoOutput
{
    public string? Message { get; set; }
}

/// <summary>
/// Echoes its typed input back - proves the endpoint's JSON body reaches the flow as its input type
/// </summary>
public sealed class EchoFlow : FlowBase<EchoInput, EchoOutput>
{
    public override string Name => nameof(EchoFlow);
    public override string Definition => "Echoes the input message (typed input tests)";
    public override EchoOutput BuildTypedOutput() => new();

    protected override Task ApplyToPipelineImpl(IPipeline pipeline, IFlowGlobalInputBag<EchoInput> inputs)
    {
        var setOutput = new SetOutputBlock();
        pipeline.AddBlock(setOutput);
        setOutput.Inputs.PropertyPath.Const = nameof(EchoOutput.Message);
        setOutput.Inputs.Value.Const = inputs.Input.Message;
        return Task.CompletedTask;
    }
}
