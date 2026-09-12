using HamsterWheel.Flows.Pipelines;

namespace HamsterWheel.Flows.Monitoring;

/// <summary>
/// The logger of a single pipeline: writes through the host's shared logger as always, and hands
/// every line to the <see cref="IFlowRunLogObserver"/>s of <b>this</b> run, with this run's identity.
/// </summary>
/// <remarks>
/// Created per pipeline by <see cref="Pipeline"/> once its flow is attached (that is when the run's
/// name is known), which is what keeps the attribution right — the shared
/// <see cref="Pipelines.IPipelineLogger"/> cannot do it, being shared. Lines written before that go
/// to the inner logger only.
/// </remarks>
public class RunLogPipelineLogger(
    IPipelineLogger inner,
    FlowRunIdentity run,
    IReadOnlyList<IFlowRunLogObserver> observers) : IPipelineLogger
{
    public void Log(string message) => Log(new FlowLogMessage(FlowLogLevel.Info, DateTimeOffset.UtcNow, message));

    public void Log(IFlowLogMessage log)
    {
        inner.Log(log);

        foreach (var observer in observers)
        {
            try
            {
                observer.Log(run, log);
            }
            catch (Exception e)
            {
                //a faulting observer must never break a flow, nor swallow the line it was given
                inner.Log(new FlowLogMessage(FlowLogLevel.Error, DateTimeOffset.UtcNow,
                    $"Flow run log observer failed: {e.Message}", null, nameof(IFlowRunLogObserver)));
            }
        }
    }

    public void SetSink(Action<IFlowLogMessage> sink) => inner.SetSink(sink);
}
