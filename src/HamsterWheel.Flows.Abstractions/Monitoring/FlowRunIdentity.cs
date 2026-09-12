namespace HamsterWheel.Flows.Monitoring;

/// <summary>
/// Identifies a single run of a flow for <see cref="IFlowProgressObserver"/> notifications.
/// </summary>
/// <param name="RunId">The run id (from the pipeline creation options, e.g. the scheduled flow id).</param>
/// <param name="FlowName">The name of the flow that is being run.</param>
/// <param name="UserId">The id of the user that triggered the run, when one is known.</param>
public sealed record FlowRunIdentity(Guid RunId, IFlowName FlowName, string? UserId)
{
    /// <summary>
    /// Whether this run is a flow started by another flow rather than by a request, a schedule or a
    /// command. A sub-flow runs under its own run id, so a host that attributes progress or log lines
    /// to the run the user is waiting for has to tell the two apart — its own run id is not enough to
    /// do that with.
    /// </summary>
    public bool IsSubFlow { get; init; }
}
