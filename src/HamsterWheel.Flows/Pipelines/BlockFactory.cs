using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.DI;
using HamsterWheel.Flows.IO;

namespace HamsterWheel.Flows.Pipelines;

public class BlockFactory(IServiceProvider services, IGlobalInputTransformer? globalInputTransformer) : IBlockFactory
{
    public T Create<T>(string? name = null, string? description = null) where T : class, IPipelineBlock, new()
    {
        CheckPermissions<T>();

        T block;
        if (name is not null)
        {
            block = new T
            {
                Id = name,
                Description = null
            };
        }
        else
        {
            block = new T
            {
                Description = null
            };
        }

        if (globalInputTransformer is not null)
        {
            block.InputTransformer = globalInputTransformer.GetTransformation<T>();
        }

        ResolveBlockDependencies(block);
        return block;
    }

    protected virtual void CheckPermissions<T>() where T : class, IPipelineBlock
    {
    }

    private void ResolveBlockDependencies(IPipelineBlock block)
    {
        if (block is INeedServices resolver)
        {
            resolver.Resolve(services);
        }
    }
}