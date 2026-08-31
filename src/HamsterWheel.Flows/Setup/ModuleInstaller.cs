using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using HamsterWheel.Flows.Auth;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Os.IO;
using HamsterWheel.Flows.Data.Serialization;
using HamsterWheel.Flows.IO;
using HamsterWheel.Flows.Monitoring;
using HamsterWheel.Flows.Pipelines;
using HamsterWheel.Flows.Runner;
using HamsterWheel.Flows.Services.IO;
using HamsterWheel.Flows.Templates;
using HamsterWheel.HLinq.Data;
using HamsterWheel.HLinq.Data.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.Flows.Setup;

[ExcludeFromCodeCoverage]
public static class ModuleInstaller
{
    public static IServiceCollection AddFlowsModule(this IServiceCollection services)
    {
        //services
        services.AddSingleton<IFlowScheduler, FlowScheduler>();
        services.AddSingleton<IRenderingService, SimpleRenderingService>();
        services.AddScoped<IPipelineFactory, PipelineFactory>();
        services.AddScoped<IBlockFactory, BlockFactory>();
        services.AddScoped<IFlowFactory, FlowFactory>();
        services.AddScoped<IFlowCoordinator, FlowCoordinator>();
        services.AddScoped<IFlowApplier, FlowApplier>();
        services.AddScoped<IFlowRunner, FlowRunner>();
        services.AddTransient<IFlowGlobalInputBag, FlowGlobalInputBag>();
        services.AddSingleton<ISerializer, BasicJsonSerializer>();
        services.AddSingleton<IFileSystem, DefaultFileSystem>();
        services.AddSingleton<IUserPermissionsService, UserPermissionsService>();
        services.AddSingleton<IPipelineLogger, PipelineLogger>();
        services.AddSingleton<IGlobalInputTransformer, DefaultGlobalInputTransformer>();
        services.AddSingleton<IExtraInputBag, DefaultExtraInputBag>();
        services.AddSingleton(Channel.CreateUnbounded<IScheduledFlowData>(new UnboundedChannelOptions
        {
            SingleReader = true,//meant to be only read from FlowBackgroundService
            SingleWriter = true,//meant to be only written by FlowScheduler
            AllowSynchronousContinuations = true
        }));

        return services;
    }

    //AddFlowsModule plus the external (HLinq) converter stack the runner chain depends on.
    //Registering this makes IFlowRunner resolvable out of the box; hosts that need custom
    //converters can register their own before or after (last registration wins)
    public static IServiceCollection AddFlowsWithDependencies(this IServiceCollection services)
    {
        services.AddFlowsModule();
        services.AddSingleton<INullKeyword, NullKeyword>();
        services.AddSingleton<EnumValueConverter>();
        services.AddSingleton<IConfigurableValueConverter, FromStringConverter>();
        services.AddSingleton<IConfigurableValueConverter, FromConvertibleConverter>();
        services.AddSingleton<IConfigurableValueConverter, FromFormattableConverter>();
        services.AddSingleton<IConfigurableValueConverter, ViaSerializationConverter>();
        services.AddSingleton<IConfigurableValueConverter, ToInterfaceConverter>();
        services.AddSingleton<IConfigurableValueConverter, EnumValueConverter>();
        services.AddSingleton<IConfigurableValueConverter, CustomTypeCombinationsConverter>();
        services.AddSingleton<IConfigurableValueConverter, NullableEnumValueConverter>();
        services.AddSingleton<IFallbackConverter, DefaultFallbackConverter>();
        services.AddSingleton<IDefaultConverter, DefaultConverter>();

        return services;
    }
}