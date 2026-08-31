using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HamsterWheel.Flows.Runner;

public partial class FlowBackgroundService(
    IFlowScheduler flowScheduler,
    IServiceProvider services,
    Channel<IScheduledFlowData> channel,
    ILogger<FlowBackgroundService> logger)
    : BackgroundService
{
    private readonly Stack<IScheduledFlowData> _scheduledPipelines = new();
    private readonly ILogger<BackgroundService> _logger = logger;
    private readonly List<Task> _runningFlows = [];

    protected IFlowScheduler Scheduler { get; } = flowScheduler;
    protected IServiceProvider Services { get; } = services;
    public Channel<IScheduledFlowData> Channel { get; } = channel;

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested || _runningFlows.Count != 0)
        {
            await Scheduler.LoadTriggers();
            try
            {
                await Task.WhenAll(ReadAllFromChannel(token), ProcessFlows(token));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }

    protected Task ScheduleFlowDirect(IScheduledFlowData message)
    {
        _scheduledPipelines.Push(message);
        return Task.CompletedTask;
    }

    protected virtual Task HandleFlowStarted(IScheduledFlowData message) => Task.CompletedTask;
    protected virtual Task HandleFlowCompleted(IScheduledFlowData message, IFlowRunResult result) => Task.CompletedTask;
    protected virtual Task HandleFlowError(IScheduledFlowData message, Exception e) => Task.CompletedTask;

    [LoggerMessage(EventId = 0, Level = LogLevel.Critical, Message = "Failed to schedule flow: {FlowName}")]
    public partial void FailedToScheduleFlow(Exception e, string flowName);

    private async Task ProcessFlows(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            while (_scheduledPipelines.TryPop(out var message))
            {
                var task = RunFlow(message, token);
                AddRunningFlow(task);
                if (token.IsCancellationRequested)
                {
                    break;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), token);
        }

        //if cancellation was requested, just wait for currently scheduled flows and shutdown
        await WaitForRunningFlows();
    }

    private async Task ReadAllFromChannel(CancellationToken token)
    {
        while (await Channel.Reader.WaitToReadAsync(token))
        {
            while (Channel.Reader.TryRead(out var flowData))
            {
                await ScheduleFlowDirect(flowData);
            }
        }
    }

    private async Task WaitForRunningFlows()
    {
        await Task.WhenAll(_runningFlows);
        RemoveFinishedFlows();
    }

    private void AddRunningFlow(Task flowTask)
    {
        _runningFlows.Add(flowTask);
        RemoveFinishedFlows();
    }

    private void RemoveFinishedFlows() => _runningFlows.RemoveAll(t => t.IsCanceled);

    private async Task RunFlow(IScheduledFlowData message, CancellationToken token = default)
    {
        await using var scope = Services.CreateAsyncScope();
        var flowRunner = scope.ServiceProvider.GetRequiredService<IFlowRunner>();
        try
        {
            await HandleFlowStarted(message);
            var result = await flowRunner.RunAsync(message, token);
            await HandleFlowCompleted(message, result);
        }
        catch (Exception e)
        {
            await HandleFlowError(message, e);
            FailedToScheduleFlow(e, message.FlowName.ToString()!);
        }
    }
}