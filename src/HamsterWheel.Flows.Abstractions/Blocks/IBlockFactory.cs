namespace HamsterWheel.Flows.Blocks;

public interface IBlockFactory
{
    T Create<T>(string? name = null, string? description = null) where T : class, IPipelineBlock, new();
}