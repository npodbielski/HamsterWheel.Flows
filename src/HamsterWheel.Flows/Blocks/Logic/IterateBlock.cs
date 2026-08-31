using System.Collections;
using HamsterWheel.Flows.Blocks.Base;

namespace HamsterWheel.Flows.Blocks.Logic;

public class IterateBlock : MultiplierPipelineBlock<object, object, IterateBlockInputTaskSource>
{
    public override Task RunForInput(object input, Action<object> pushOutput, CancellationToken token)
    {
        if (input is IEnumerable enumerable)
        {
            foreach (var obj in enumerable)
            {
                pushOutput(obj);
            }

            Log("Finished iterating input.");
            return Task.CompletedTask;
        }

        Log("Input is not iterable");
        return Task.CompletedTask;
    }
}