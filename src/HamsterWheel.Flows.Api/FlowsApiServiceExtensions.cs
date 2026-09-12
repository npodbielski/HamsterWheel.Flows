using HamsterWheel.Flows.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace HamsterWheel.Flows.Api;

public static class FlowsApiServiceExtensions
{
    /// <summary>
    /// Registers Flows.Api package services and Background Service. Does not override consuming
    /// project overrides.
    /// </summary>
    public static IServiceCollection AddFlowsApi(this IServiceCollection services)
    {
        services.TryAddSingleton<IFlowRunStarter, ChannelFlowRunStarter>();

        if (!HasFlowBackgroundService(services))
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, FlowBackgroundService>());
        }

        return services;
    }

    private static bool HasFlowBackgroundService(IServiceCollection services) =>
        services.Any(d => d.ServiceType == typeof(IHostedService)
            && (d.ImplementationType is { } implementationType
                    && typeof(FlowBackgroundService).IsAssignableFrom(implementationType)
                || d.ImplementationInstance is FlowBackgroundService));
}
