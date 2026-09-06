namespace HamsterWheel.Flows.Api;

public sealed record FlowRunOutcome(
    FlowName FlowName,
    FlowRunStatus Status,
    object? Output,
    Exception? Error,
    Guid? RunId = null);

/// <summary>
/// Starts and tracks flow runs. The package ships a direct implementation (ChannelFlowRunStarter);
/// hosts can provide their own (e.g. on top of a message bus).
/// </summary>
public interface IFlowRunStarter
{
    Task<FlowRunOutcome> RunAsync(
        FlowName flowName,
        string? userId,
        object? input,
        TimeSpan runTimeout,
        CancellationToken token);
}
