using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.Flows;

[ExcludeFromCodeCoverage]
public static class FlowServiceCollectionExtensions
{
    public static IServiceCollection AddFlow<TFlow>(this IServiceCollection services) where TFlow : class, IFlow
    {
        var serviceKey = FlowName.NormalizeFlowName(typeof(TFlow).Name);
        //flow need to be recreated for each single pipeline 
        services.AddTransient<IFlow, TFlow>();
        return services.AddKeyedTransient<IFlow, TFlow>(serviceKey);
    }
}