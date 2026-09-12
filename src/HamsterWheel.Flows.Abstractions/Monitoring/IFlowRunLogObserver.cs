namespace HamsterWheel.Flows.Monitoring;

/// <summary>
/// Observes the log lines of flow runs together with the run that wrote them — the log counterpart
/// of <see cref="IFlowProgressObserver"/>, and opt-in the same way: nobody resolves it, nobody pays
/// for it.
/// </summary>
/// <remarks>
/// <para>
/// This is not <see cref="Pipelines.IPipelineLogger.SetSink"/>: there is one logger shared by every
/// pipeline, so a sink registered there sees the lines of <em>all</em> pipelines, is called once per
/// registered sink, and carries no run identity. An observer is called once per line, with the
/// identity of the run that produced it — the only correct way to attribute a log line to a run.
/// </para>
/// <para>
/// Implementations must be thread-safe (blocks log concurrently) and must not throw — a faulting
/// observer is logged and otherwise ignored, so that it can never break a flow.
/// </para>
/// </remarks>
public interface IFlowRunLogObserver
{
    /// <summary>
    /// A <paramref name="log"/> line written by the run identified by <paramref name="run"/>.
    /// </summary>
    void Log(FlowRunIdentity run, IFlowLogMessage log);
}
