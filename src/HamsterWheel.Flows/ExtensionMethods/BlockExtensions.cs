using HamsterWheel.Flows.Blocks;
using HamsterWheel.HLinq.Data.Converters;

namespace HamsterWheel.Flows;

public static class BlockExtensions
{
    public static void If(this IPipelineBlock block, object? condition) =>
        block.Condition = Task.FromResult(DefaultConverter.Instance.ConvertToBoolean(condition, false)!.Value);
}