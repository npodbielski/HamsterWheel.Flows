using System.Text.Json;
using HamsterWheel.Flows;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HamsterWheel.Flows.Api;

public static class FlowsApiExtensions
{
    public const string DefaultRouteTemplate = "/api/flows/{flowName}/run";
    public static readonly TimeSpan DefaultRunTimeout = TimeSpan.FromSeconds(55);

    /// <summary>
    /// Maps POST {RouteTemplate} (default /api/flows/{flowName}/run) - the flow is resolved by name
    /// from AddFlow&lt;T&gt;() registrations.
    /// </summary>
    public static IEndpointConventionBuilder MapFlowRunEndpoint(
        this IEndpointRouteBuilder endpoints, MapFlowRunEndpointOptions? options = null) =>
        MapFlowRunEndpointInternal(endpoints, options, successResult: outcome => Results.Ok(outcome.Output));

    /// <summary>
    /// Maps the flow run endpoint like MapFlowRunEndpoint, but enforces the flow's output type: a
    /// successful run whose output is not TOutput (or is missing) throws
    /// MismatchedFlowOutputTypeException&lt;TOutput&gt; instead of returning a mismatched payload.
    /// </summary>
    public static IEndpointConventionBuilder MapFlowRunEndpoint<TOutput>(
        this IEndpointRouteBuilder endpoints, MapFlowRunEndpointOptions? options = null) =>
        MapFlowRunEndpointInternal(endpoints, options, successResult: outcome =>
        {
            if (outcome.Output is null)
            {
                //a flow with no output cannot satisfy a typed endpoint
                throw new MismatchedFlowOutputTypeException<TOutput>(outcome.FlowName.ToString(), null);
            }

            if (!typeof(TOutput).IsAssignableFrom(outcome.Output.GetType()))
            {
                throw new MismatchedFlowOutputTypeException<TOutput>(outcome.FlowName.ToString(),
                    outcome.Output.GetType());
            }

            return Results.Ok(outcome.Output);
        });

    private static IEndpointConventionBuilder MapFlowRunEndpointInternal(
        IEndpointRouteBuilder endpoints, MapFlowRunEndpointOptions? options, Func<FlowRunOutcome, IResult> successResult)
    {
        var route = options?.RouteTemplate ?? DefaultRouteTemplate;

        return endpoints.MapPost(route, async (HttpContext context, IFlowRunStarter? starter, string flowName,
            JsonElement? input, CancellationToken token) =>
        {
            var runStarter = starter ?? throw new InvalidOperationException(
                $"{nameof(IFlowRunStarter)} is not registered. " +
                $"Call services.AddFlowsApi() (or register your own {nameof(IFlowRunStarter)}) before mapping the flow run endpoint.");
            var flowInput = input is null || input.Value.ValueKind == JsonValueKind.Undefined
                ? null
                : input.Value.Deserialize<object?>();
            var outcome = await runStarter.RunAsync(
                FlowName.FromString(flowName),
                options?.GetUserId?.Invoke(context),
                flowInput,
                options?.RunTimeout ?? DefaultRunTimeout,
                token);
            return MapOutcome(outcome, successResult);
        });
    }

    private static IResult MapOutcome(FlowRunOutcome outcome, Func<FlowRunOutcome, IResult> successResult)
    {
        switch (outcome.Status)
        {
            case FlowRunStatus.Success:
                return successResult(outcome);
            case FlowRunStatus.Failed when outcome.Error is FlowNotFoundException:
                return Results.NotFound(new
                {
                    error = "Flow not found",
                    flowName = outcome.FlowName.ToString()
                });
            case FlowRunStatus.Failed:
                return Results.Problem(
                    title: "Flow run failed",
                    detail: outcome.Error?.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            case FlowRunStatus.TimedOut:
                //no terminal result within the budget; the run itself may still be in progress
                return Results.Accepted(value: new
                {
                    flowName = outcome.FlowName.ToString(),
                    runId = outcome.RunId,
                    message =
                        "Flow run has not reached a terminal result within the timeout budget and may still be running."
                });
            default:
                return Results.Problem(statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
