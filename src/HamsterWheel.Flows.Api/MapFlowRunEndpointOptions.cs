using Microsoft.AspNetCore.Http;

namespace HamsterWheel.Flows.Api;

public sealed record MapFlowRunEndpointOptions
{
    public string RouteTemplate { get; init; } = FlowsApiExtensions.DefaultRouteTemplate;
    public Func<HttpContext, string?>? GetUserId { get; init; }
    public TimeSpan RunTimeout { get; init; } = FlowsApiExtensions.DefaultRunTimeout;

    /// <summary>
    /// Template of the host's run resource, e.g. "/core/flows/{flowName}/run/{runId}".
    /// When set, a run that reached no terminal result within <see cref="RunTimeout"/> answers
    /// 201 Created + Location (the run resource exists and is queryable); without it the same case
    /// answers 202 Accepted. Falls back to 202 when the starter reported no run id.
    /// </summary>
    public string? RunUrlTemplate { get; init; }

    /// <summary>
    /// Full control over the pending result; wins over <see cref="RunUrlTemplate"/>.
    /// </summary>
    public Func<FlowRunOutcome, IResult>? TimedOutResult { get; init; }
}
