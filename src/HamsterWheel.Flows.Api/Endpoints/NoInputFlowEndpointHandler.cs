using HamsterWheel.Flows.Auth;
using Microsoft.AspNetCore.Http;

namespace HamsterWheel.Flows.Api.Endpoints;

/// <summary>
/// Handler of a flow endpoint with no input and a declared output type — e.g. a cron-triggered
/// refresh triggered by hand.
/// </summary>
public abstract class NoInputFlowEndpointHandler<TOutput>(IFlowRunStarter flowRunStarter, IFlowUserService currentUser)
    : FlowRunStarterHandler(flowRunStarter, currentUser), INoInputFlowEndpointHandler
{
    public async Task<IResult> Start(
        Func<FlowRunOutcome, IResult>? pending = null,
        CancellationToken token = default)
        => FlowRunResults.Map<TOutput>(await RunAsync(null, token), pending);
}
