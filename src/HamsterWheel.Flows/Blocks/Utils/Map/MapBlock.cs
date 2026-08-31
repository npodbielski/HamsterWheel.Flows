using HamsterWheel.Flows.Blocks.Base;
using HamsterWheel.HLinq;

namespace HamsterWheel.Flows.Blocks.Utils;

public class MapBlock : PipelineBlock<MapInput, object?, MapInputTaskSource>
{
    public override Task<object?> RunForInput(MapInput input, CancellationToken token)
    {
        return Task.FromResult(input.Operation switch
        {
            MapOperation.Property => input.Object.ExecuteHLinq($"x.{input.Map}"),
            MapOperation.HLinq => input.Object.ExecuteHLinq(input.Map),
            //TODO: it should be possible to reuse the same mechanism as in SetPropertyBlock to map data here
            _ => throw new IncorrectMapOperationValueException(input.Operation)
        });
    }
}