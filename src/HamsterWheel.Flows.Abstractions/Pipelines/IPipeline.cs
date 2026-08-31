using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Runner;

namespace HamsterWheel.Flows.Pipelines;

public interface IPipeline
{
    IPipelineLogger Logger { get; }
    Task Initialized { get; }
    IFlowContext FlowContext { get; }
    IPipelineCreationOptions Options { get; }
    void AddBlock(IPipelineBlock block);
    T AddBlock<T>(string? name = null, string? description = null) where T : class, IPipelineBlock, new();
    Task<object?> Run(CancellationToken token = default);
    void AttachCoordinator(IFlowCoordinator coordinator);
    void AttachFlowContext(IFlowContext flowContext);
}