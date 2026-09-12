using HamsterWheel.Flows.Blocks.Base;
using HamsterWheel.Flows.IO;

namespace HamsterWheel.Flows.Blocks.Utils;

/// <summary>
/// Waits for the given number of seconds, produces no output
/// </summary>
public class DelayBlock : NoOutPipelineBlock<int, SingleInputTaskSource<int>>
{
    public override async Task RunForInput(int seconds, CancellationToken token)
    {
        if (seconds <= 0)
        {
            Log("Delay not longer than 0 seconds, nothing to wait for.");
            return;
        }

        Log($"Delaying for {seconds} second(s)...");
        await Task.Delay(TimeSpan.FromSeconds(seconds), token);
        Log("Delay finished.");
    }
}
