using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using HamsterWheel.Flows;
using HamsterWheel.Flows.IntegrationTests.Flows;
using HamsterWheel.Flows.Runner;
using HamsterWheel.Flows.Setup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.Flows.IntegrationTests.Fixtures;

public static class FlowApiBuilder
{
    public static WebApplication Build()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{GetFreePort()}");
        builder.Services.AddFlowsWithDependencies();
        builder.Services.AddFlow<GreetFlow>();
        builder.Services.AddFlow<FailingFlow>();
        var app = builder.Build();

        app.Use(async (context, next) =>
        {
            try
            {
                await next();
            }
            catch (FlowNotFoundException)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
            }
        });

        app.MapPost("/api/flows/{flowName}/run",
            async (string flowName, JsonElement? input, IServiceProvider services, CancellationToken token) =>
            {
                var flowInput = input is null || input.Value.ValueKind == JsonValueKind.Undefined
                    ? null
                    : input.Value.Deserialize<object?>();
                var message = ScheduledFlowData.New(FlowName.FromString(flowName), "api-user", flowInput);
                await using var scope = services.CreateAsyncScope();
                var runner = scope.ServiceProvider.GetRequiredService<IFlowRunner>();
                var result = await runner.RunAsync(message, token);
                return Results.Ok(new
                {
                    FlowName = result.FlowName.Name,
                    Output = result.Output
                });
            });

        return app;
    }

    internal static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
