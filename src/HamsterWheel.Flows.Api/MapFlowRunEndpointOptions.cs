using Microsoft.AspNetCore.Http;

namespace HamsterWheel.Flows.Api;

public sealed record MapFlowRunEndpointOptions
{
    public string RouteTemplate { get; init; } = FlowsApiExtensions.DefaultRouteTemplate;
    public Func<HttpContext, string?>? GetUserId { get; init; }
    public TimeSpan RunTimeout { get; init; } = FlowsApiExtensions.DefaultRunTimeout;
}
