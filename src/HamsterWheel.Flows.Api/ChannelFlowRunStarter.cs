using System.Threading.Channels;
using HamsterWheel.Flows.Runner;

namespace HamsterWheel.Flows.Api;

/// <summary>
/// Starts flow runs via the flow channel (executed by FlowBackgroundService)
/// </summary>
public sealed class ChannelFlowRunStarter(Channel<IScheduledFlowData> channel) : IFlowRunStarter
{
    public async Task<FlowRunOutcome> RunAsync(
        FlowName flowName, string? userId, object? input,
        TimeSpan runTimeout, CancellationToken token)
    {
        var completion = new TaskCompletionSource<IFlowRunResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var runId = Guid.NewGuid();
        var message = new ScheduledFlowData(runId, DateTimeOffset.UtcNow, flowName,
            userId, input, false)
        {
            Completion = completion
        };
        message.Validate();
        channel.Writer.TryWrite(message);

        try
        {
            var result = await completion.Task.WaitAsync(runTimeout, token);
            return new FlowRunOutcome(flowName, FlowRunStatus.Success, result.Output, null, runId);
        }
        catch (TimeoutException)
        {
            return new FlowRunOutcome(flowName, FlowRunStatus.TimedOut, null, null, runId);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            //the run was cancelled by something else (e.g. host shutdown) - no terminal result in time
            return new FlowRunOutcome(flowName, FlowRunStatus.TimedOut, null, null, runId);
        }
        catch (Exception e)
        {
            return new FlowRunOutcome(flowName, FlowRunStatus.Failed, null, e, runId);
        }
    }
}
