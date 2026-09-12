using HamsterWheel.Flows.Auth;
using Microsoft.AspNetCore.Http;

namespace HamsterWheel.Flows.Api.Endpoints;

/// <summary>
/// Handler of a flow endpoint with an input and a declared output type: the output type of the flow is
/// checked and mapped (<see cref="FlowRunResults.Map{TOutput}"/>) — the endpoint decides only what a
/// pending run is worth.
/// </summary>
public abstract class FlowEndpointHandler<TInput, TOutput>(IFlowRunStarter flowRunStarter, IFlowUserService currentUser)
    : FlowRunStarterHandler(flowRunStarter, currentUser), IFlowEndpointHandler<TInput, TOutput>
{
    public async Task<IResult> Start(
        TInput input,
        Func<FlowRunOutcome, IResult>? pending = null,
        CancellationToken token = default)
        => FlowRunResults.Map<TOutput>(await RunAsync(input, token), pending);
}
