using HamsterWheel.Flows.Blocks.Base;
using HamsterWheel.Flows.IO;

namespace HamsterWheel.Flows.Blocks;

public class LogBlock : NoOutPipelineBlock<string, SingleInputTaskSource<string>>
{
    public override Task RunForInput(string input, CancellationToken token)
    {
        Log(input);
        return Task.CompletedTask;
    }
}