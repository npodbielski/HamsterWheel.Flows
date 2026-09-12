using HamsterWheel.Flows.Auth;
using Microsoft.AspNetCore.Http;

namespace HamsterWheel.Flows.Api.Endpoints;

/// <summary>
/// Handler of a flow endpoint with neither input nor declared output type.
/// </summary>
public abstract class NoInputNoOutputFlowEndpointHandler(IFlowRunStarter flowRunStarter, IFlowUserService currentUser)
    : FlowRunStarterHandler(flowRunStarter, currentUser), INoInputFlowEndpointHandler
{
    public async Task<IResult> Start(
        Func<FlowRunOutcome, IResult>? pending = null,
        CancellationToken token = default)
        => FlowRunResults.Map(await RunAsync(null, token), FlowRunResults.OkOutput, pending);
}
