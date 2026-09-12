using Microsoft.AspNetCore.Http;

namespace HamsterWheel.Flows.Api.Endpoints;

/// <summary>
/// The same as <see cref="IFlowEndpointHandler{TInput, TOutput}"/> for a flow with no input —
/// e.g. a cron-triggered refresh triggered by hand. Implementations declare their own output type
/// (<see cref="NoInputFlowEndpointHandler{TOutput}"/> and friends), so this is also the constraint
/// a host's endpoint uses when it resolves the concrete handler class of the flow.
/// </summary>
public interface INoInputFlowEndpointHandler
{
    /// <summary>Name of the flow to run.</summary>
    string FlowName { get; }

    /// <summary>
    /// Starts the run and maps its outcome to the answer. <paramref name="pending"/> decides what a
    /// run still waiting for its result is worth; <c>null</c> answers 202 Accepted.
    /// </summary>
    Task<IResult> Start(
        Func<FlowRunOutcome, IResult>? pending = null,
        CancellationToken token = default);
}
