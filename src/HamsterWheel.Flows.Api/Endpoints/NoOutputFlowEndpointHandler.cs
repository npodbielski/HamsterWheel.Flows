using HamsterWheel.Flows.Auth;
using Microsoft.AspNetCore.Http;

namespace HamsterWheel.Flows.Api.Endpoints;

/// <summary>
/// Handler of a flow endpoint with an input and no declared output type: the run's output, if any, is
/// answered as 200 OK (<see cref="FlowRunResults.OkOutput"/>) without being checked.
/// </summary>
public abstract class NoOutputFlowEndpointHandler<TInput>(IFlowRunStarter flowRunStarter, IFlowUserService currentUser)
    : FlowRunStarterHandler(flowRunStarter, currentUser), INoOutputFlowEndpointHandler<TInput>
{
    public async Task<IResult> Start(
        TInput input,
        Func<FlowRunOutcome, IResult>? pending = null,
        CancellationToken token = default)
        => FlowRunResults.Map(await RunAsync(input, token), FlowRunResults.OkOutput, pending);
}
