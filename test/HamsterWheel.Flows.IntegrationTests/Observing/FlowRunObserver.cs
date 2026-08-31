using System.Threading.Channels;
using HamsterWheel.Flows.Runner;

namespace HamsterWheel.Flows.IntegrationTests.Observing;

public sealed class FlowRunObserver
{
    private readonly Channel<CompletedFlowRun> _completed = Channel.CreateUnbounded<CompletedFlowRun>();
    private readonly Channel<FailedFlowRun> _failed = Channel.CreateUnbounded<FailedFlowRun>();
    private readonly TaskCompletionSource _firstRunTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public sealed record CompletedFlowRun(IScheduledFlowData Message, IFlowRunResult Result);
    public sealed record FailedFlowRun(IScheduledFlowData Message, Exception Error);

    public void RecordCompleted(IScheduledFlowData message, IFlowRunResult result)
    {
        _completed.Writer.TryWrite(new CompletedFlowRun(message, result));
        _firstRunTcs.TrySetResult();
    }

    public void RecordError(IScheduledFlowData message, Exception error)
    {
        _failed.Writer.TryWrite(new FailedFlowRun(message, error));
        _firstRunTcs.TrySetResult();
    }

    public async Task<CompletedFlowRun> WaitNextCompletedAsync(TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await _firstRunTcs.Task.WaitAsync(cts.Token);
            while (await _completed.Reader.WaitToReadAsync(cts.Token))
            {
                if (_completed.Reader.TryRead(out var run))
                {
                    return run;
                }
            }
        }
        catch (OperationCanceledException)
        {
            //expected on timeout
        }

        throw new TimeoutException("No completed flow run was observed");
    }

    public async Task<FailedFlowRun> WaitNextErrorAsync(TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await _firstRunTcs.Task.WaitAsync(cts.Token);
            while (await _failed.Reader.WaitToReadAsync(cts.Token))
            {
                if (_failed.Reader.TryRead(out var run))
                {
                    return run;
                }
            }
        }
        catch (OperationCanceledException)
        {
            //expected on timeout
        }

        throw new TimeoutException("No failed flow run was observed");
    }
}
