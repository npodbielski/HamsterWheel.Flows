using Microsoft.AspNetCore.Http;

namespace HamsterWheel.Flows.Api.Endpoints;

/// <summary>
/// The same as <see cref="IFlowEndpointHandler{TInput, TOutput}"/> for a flow with no declared
/// output type: whatever the run produced is answered as 200 OK
/// (<see cref="FlowRunResults.OkOutput"/>) - the caller was never told what the body would be.
/// </summary>
public interface INoOutputFlowEndpointHandler<in TInput>
{
    /// <summary>Name of the flow to run.</summary>
    string FlowName { get; }

    /// <summary>
    /// Starts the run and maps its outcome to the answer. <paramref name="pending"/> decides what a
    /// run still waiting for its result is worth; <c>null</c> answers 202 Accepted.
    /// </summary>
    Task<IResult> Start(
        TInput input,
        Func<FlowRunOutcome, IResult>? pending = null,
        CancellationToken token = default);
}
