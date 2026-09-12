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
    /// from AddFlow&lt;T&gt;() registrations. Result mapping: see <see cref="FlowRunResults"/>.
    /// </summary>
    public static IEndpointConventionBuilder MapFlowRunEndpoint(
        this IEndpointRouteBuilder endpoints, MapFlowRunEndpointOptions? options = null) =>
        MapFlowRunEndpointInternal(endpoints, options,
            (outcome, timedOut) => FlowRunResults.Map(outcome, outcome => Results.Ok(outcome.Output), timedOut));

    /// <summary>
    /// Maps the flow run endpoint like MapFlowRunEndpoint, but enforces the flow's output type: a
    /// successful run whose output is not TOutput (or is missing) throws
    /// MismatchedFlowOutputTypeException&lt;TOutput&gt; instead of returning a mismatched payload.
    /// </summary>
    public static IEndpointConventionBuilder MapFlowRunEndpoint<TOutput>(
        this IEndpointRouteBuilder endpoints, MapFlowRunEndpointOptions? options = null) =>
        MapFlowRunEndpointInternal(endpoints, options,
            (outcome, timedOut) => FlowRunResults.Map<TOutput>(outcome, timedOut));

    private static IEndpointConventionBuilder MapFlowRunEndpointInternal(
        IEndpointRouteBuilder endpoints, MapFlowRunEndpointOptions? options,
        Func<FlowRunOutcome, Func<FlowRunOutcome, IResult>?, IResult> mapOutcome)
    {
        var route = options?.RouteTemplate ?? DefaultRouteTemplate;

        return endpoints.MapPost(route, async (HttpContext context, IFlowRunStarter? starter, string flowName,
            JsonElement? input, CancellationToken token) =>
        {
            var runStarter = starter ?? throw new InvalidOperationException(
                $"{nameof(IFlowRunStarter)} is not registered. " +
                $"Call services.AddFlowsApi() (or register your own {nameof(IFlowRunStarter)}) before mapping the flow run endpoint.");
            //the body stays a JsonElement: this route resolves the flow by name at request time, so
            //the input type is unknown here - FlowApplier coerces it to the flow's input type
            var flowInput = input is { } body && body.ValueKind is not (JsonValueKind.Undefined or JsonValueKind.Null)
                ? (object)body
                : null;
            var outcome = await runStarter.RunAsync(
                FlowName.FromString(flowName),
                options?.GetUserId?.Invoke(context),
                flowInput,
                options?.RunTimeout ?? DefaultRunTimeout,
                token);
            return mapOutcome(outcome, options?.TimedOutResult ?? TimedOutMapper(options));
        });
    }

    private static Func<FlowRunOutcome, IResult>? TimedOutMapper(MapFlowRunEndpointOptions? options) =>
        options?.RunUrlTemplate is { } template ? outcome => FlowRunResults.Pending(outcome, template) : null;
}
