using Microsoft.AspNetCore.Http;

namespace HamsterWheel.Flows.Api.Endpoints;

/// <summary>
/// Starts the run behind a generated flow endpoint with an input and a declared output type, and
/// answers it: the run is started through <see cref="IFlowRunStarter"/>, the output type is checked
/// and the outcome mapped to the result by <see cref="FlowRunResults.Map{TOutput}"/>.
/// </summary>
/// <remarks>
/// This is the generator's DI vocabulary (<c>AddScoped&lt;IFlowEndpointHandler&lt;TInput, TOutput&gt;,
/// XHandler&gt;()</c>): one handler per flow endpoint, resolved by the endpoint at request time.
/// </remarks>
public interface IFlowEndpointHandler<in TInput, TOutput>
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
