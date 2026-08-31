using HamsterWheel.Flows.Blocks.Base;

namespace HamsterWheel.Flows.Blocks.Utils;

public class SetOutputBlock : NoOutPipelineBlock<SetOutputInput, SetOutputTaskSource>
{
    private readonly SetPropertyBlock _setPropertyBlock = new() { Id = "SetProperty" };

    public override async Task RunForInput(SetOutputInput input, CancellationToken token)
    {
        var output = Context.Output ?? throw new SetOutputBlockUsedOnNullOutputException();
        await _setPropertyBlock.RunForInput(input.ToParentCommand(output), token);
    }
}