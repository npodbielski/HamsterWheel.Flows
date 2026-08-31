using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Pipelines;

namespace HamsterWheel.Flows.IO;

public interface IGlobalInputTransformer
{
    void RegisterForBlock<TBlock, TInputSource, TInput>(Func<IPipelineLogger, TInput?, TInput?> transformInput)
        where TBlock : IBlockInput<TInputSource>
        where TInputSource : class, IInputSourceEnumerator<TInput>
        where TInput : class;

    Func<IPipelineLogger, object?, object?>? GetTransformation<TBlock>();
}