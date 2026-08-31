namespace HamsterWheel.Flows.Runner;

public interface IFlowRunResult
{
    /// <summary>
    /// Output of flow pipeline
    /// </summary>
    object? Output { get; }
    /// <summary>
    /// It may happen that original flow was not found and fallback flow was run instead
    /// </summary>
    IFlowName FlowName { get; }
}