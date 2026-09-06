using System.Net;
using System.Net.Sockets;
using HamsterWheel.Flows;
using HamsterWheel.Flows.Api;
using HamsterWheel.Flows.IntegrationTests.Flows;
using HamsterWheel.Flows.IntegrationTests.Observing;
using HamsterWheel.Flows.Setup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.Flows.IntegrationTests.Fixtures;

/// <summary>
/// Host with the package's MapFlowRunEndpoint + ChannelFlowRunStarter
/// (FlowApiBuilder covers the inline IFlowRunner path instead)
/// </summary>
public static class FlowRunEndpointBuilder
{
    public static WebApplication Build()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{FlowApiBuilder.GetFreePort()}");
        builder.Services.AddFlowsWithDependencies();
        builder.Services.AddFlow<GreetFlow>();
        builder.Services.AddFlow<FailingFlow>();
        builder.Services.AddFlow<MismatchFlow>();
        builder.Services.AddSingleton(new FlowRunObserver());
        builder.Services.AddHostedService<CapturingFlowBackgroundService>();
        builder.Services.AddFlowsApi();
        var app = builder.Build();

        //hosts render flow exceptions to responses themselves - here: 500 with the message
        app.Use(async (context, next) =>
        {
            try
            {
                await next();
            }
            catch (FlowException e)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync(e.Message);
            }
        });

        app.MapFlowRunEndpoint(new MapFlowRunEndpointOptions { GetUserId = _ => "endpoint-user" });
        app.MapFlowRunEndpoint<GreetFlowOutput>(new MapFlowRunEndpointOptions
        {
            RouteTemplate = "/api/typed/flows/{flowName}/run",
            GetUserId = _ => "endpoint-user"
        });

        return app;
    }
}
