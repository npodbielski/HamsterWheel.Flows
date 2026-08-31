namespace HamsterWheel.Flows.Runner;

public interface IFlowRunner
{
    Task<IFlowRunResult> RunAsync(IScheduledFlowData message, CancellationToken stoppingToken);
}