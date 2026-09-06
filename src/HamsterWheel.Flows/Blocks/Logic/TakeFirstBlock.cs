using System.Collections;
using HamsterWheel.Flows.Blocks.Base;
using HamsterWheel.Flows.IO;

namespace HamsterWheel.Flows.Blocks.Logic;

public class TakeFirstBlock : PipelineBlock<object, object?, SingleInputTaskSource<object>>
{
    public override Task<object?> RunForInput(object input, CancellationToken token)
    {
        if (input is IEnumerable enumerable)
        {
            Log("Finished iterating input.");
            return Task.FromResult(enumerable.Cast<object?>().First());
        }

        Log("Input is not iterable. Returning input object...");
        return Task.FromResult<object?>(input);
    }
}
