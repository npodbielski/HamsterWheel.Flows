using HamsterWheel.Flows.IO;
using HamsterWheel.Flows.Pipelines;

namespace HamsterWheel.Flows.Blocks;

public interface IPipelineBlock
{
    IInputInfo Inputs { get; }
    public string Id { get; init; }
    public string? Description { get; init; }
#if !NETSTANDARD2_0
    // ReSharper disable once UnassignedGetOnlyAutoProperty
    public static virtual string[]? RequiredPermissions { get; }
#endif
    public Func<IPipelineLogger, object?, object?>? InputTransformer { get; set; }
    Task Run(CancellationToken token);
    internal Task Completion { get; }
    Task<bool>? Condition { get; set; }
    void Init(IFlowContext context);
    void TriggerAfter(Task task);
    void TriggerAfter(params IPipelineBlock[] block);
}

public interface IPipelineBlock<TOutput> : IPipelineBlock
{
    IBlockResult<TOutput> Result { get; }
    bool SingleOutput { get; }
}