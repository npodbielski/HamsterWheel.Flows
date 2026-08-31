using HamsterWheel.Flows.Blocks.Base;

namespace HamsterWheel.Flows.Blocks.Os.IO;

public class JoinPathsBlock : PipelineBlock<JoinPathsInput, string, JoinPathsInputTaskSource>
{
    public override Task<string> RunForInput(JoinPathsInput input, CancellationToken token)
    {
        Log($"Joining paths: '{input.First}' and '{input.Second}'");
        var join = Path.Combine(input.First, input.Second);
        Log($"result is: '{join}'");

        return Task.FromResult(join);
    }
}