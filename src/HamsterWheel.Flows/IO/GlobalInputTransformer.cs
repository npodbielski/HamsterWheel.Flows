using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Pipelines;

namespace HamsterWheel.Flows.IO;

public abstract class GlobalInputTransformer : IGlobalInputTransformer
{
    protected readonly Dictionary<Type, Func<IPipelineLogger, object?, object?>> Transformations = new();

    public virtual void RegisterForBlock<TBlock, TInputSource, TInput>(
        Func<IPipelineLogger, TInput?, TInput?> transformInput)
        where TBlock : IBlockInput<TInputSource>
        where TInputSource : class, IInputSourceEnumerator<TInput>
        where TInput : class =>
        Transformations[typeof(TBlock)] = object? (l, i) => transformInput(l, (TInput?)i);

    public virtual Func<IPipelineLogger, object?, object?>? GetTransformation<TBlock>() =>
        Transformations.GetValueOrDefault(typeof(TBlock));
}