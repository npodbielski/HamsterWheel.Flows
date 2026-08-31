using System.Threading.Channels;
using HamsterWheel.Flows.Runner;
using Microsoft.Extensions.Logging;

namespace HamsterWheel.Flows.IntegrationTests.Observing;

public sealed class CapturingFlowBackgroundService(
    IFlowScheduler flowScheduler,
    IServiceProvider services,
    Channel<IScheduledFlowData> channel,
    ILogger<FlowBackgroundService> logger,
    FlowRunObserver observer)
    : FlowBackgroundService(flowScheduler, services, channel, logger)
{
    protected override Task HandleFlowCompleted(IScheduledFlowData message, IFlowRunResult result)
    {
        observer.RecordCompleted(message, result);
        return Task.CompletedTask;
    }

    protected override Task HandleFlowError(IScheduledFlowData message, Exception e)
    {
        observer.RecordError(message, e);
        return Task.CompletedTask;
    }
}
