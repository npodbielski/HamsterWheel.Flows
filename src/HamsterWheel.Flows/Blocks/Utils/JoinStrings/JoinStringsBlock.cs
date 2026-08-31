using HamsterWheel.Flows.Blocks.Base;

namespace HamsterWheel.Flows.Blocks.Utils;

public class JoinStringsBlock : PipelineBlock<JoinStringsInput, string, JoinStringsTaskSource>
{
    public override Task<string> RunForInput(JoinStringsInput input, CancellationToken token)
    {
        var join = string.Join(input.Delimiter ?? "", input.Chunks);
        Log($"result is: '{join}'");
        return Task.FromResult(join);
    }
}