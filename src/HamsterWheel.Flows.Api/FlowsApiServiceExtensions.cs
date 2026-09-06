using HamsterWheel.Flows.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HamsterWheel.Flows.Api;

public static class FlowsApiServiceExtensions
{
    /// <summary>
    /// Registers the default IFlowRunStarter (ChannelFlowRunStarter) and, when no FlowBackgroundService
    /// derivative is registered yet, registers FlowBackgroundService as the hosted service reading the
    /// flow channel. Hosts with a custom FlowBackgroundService derivative should register it before this call.
    /// </summary>
    public static IServiceCollection AddFlowsApi(this IServiceCollection services)
    {
        services.AddSingleton<IFlowRunStarter, ChannelFlowRunStarter>();
        if (!HasFlowBackgroundService(services))
        {
            services.AddHostedService<FlowBackgroundService>();
        }

        return services;
    }

    private static bool HasFlowBackgroundService(IServiceCollection services) =>
        services.Any(d => d.ServiceType == typeof(IHostedService)
            && (d.ImplementationType is { } implementationType
                    && typeof(FlowBackgroundService).IsAssignableFrom(implementationType)
                || d.ImplementationInstance is FlowBackgroundService));
}
