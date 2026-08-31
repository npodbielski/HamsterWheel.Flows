using System.Collections.Concurrent;
using HamsterWheel.Flows;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Monitoring;

namespace HamsterWheel.Flows.IntegrationTests.Observing;

/// <summary>
/// Captures IFlowProgressObserver notifications from a running host for assertions.
/// Thread-safe - blocks complete concurrently.
/// </summary>
public sealed class FlowProgressObserverCapture : IFlowProgressObserver
{
    private readonly ConcurrentQueue<string> _events = new();
    private readonly TaskCompletionSource _finished = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Guid? RunId { get; private set; }
    public IFlowName? FlowName { get; private set; }
    public int BlockCount { get; private set; }
    public object? Output { get; private set; }
    public Exception? Error { get; private set; }

    public IReadOnlyList<string> Events => [.. _events];

    public void FlowStarted(FlowRunIdentity run, int blockCount)
    {
        RunId = run.RunId;
        FlowName = run.FlowName;
        BlockCount = blockCount;
        _events.Enqueue(nameof(IFlowProgressObserver.FlowStarted));
    }

    public void BlockCompleted(FlowRunIdentity run, IPipelineBlock block) =>
        _events.Enqueue($"BlockCompleted:{block.Id}");

    public void BlockFailed(FlowRunIdentity run, IPipelineBlock block, Exception exception) =>
        _events.Enqueue($"BlockFailed:{block.Id}");

    public void FlowCompleted(FlowRunIdentity run, object? output)
    {
        Output = output;
        _events.Enqueue(nameof(IFlowProgressObserver.FlowCompleted));
        _finished.TrySetResult();
    }

    public void FlowFailed(FlowRunIdentity run, Exception error)
    {
        Error = error;
        _events.Enqueue(nameof(IFlowProgressObserver.FlowFailed));
        _finished.TrySetResult();
    }

    public async Task WaitFlowFinishedAsync(TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await _finished.Task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException("No flow finished notification was received from the progress observer");
        }
    }
}
