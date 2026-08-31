using HamsterWheel.Flows;
using HamsterWheel.Flows.IntegrationTests.Flows;
using HamsterWheel.Flows.IntegrationTests.Observing;
using HamsterWheel.Flows.Monitoring;
using HamsterWheel.Flows.Runner;
using HamsterWheel.Flows.Setup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HamsterWheel.Flows.IntegrationTests.Fixtures;

public static class FlowHostBuilder
{
    public static IHost Build(FlowRunObserver observer, bool registerCronTrigger = true,
        IFlowProgressObserver? progressObserver = null)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddFlowsWithDependencies();
        builder.Services.AddFlow<GreetFlow>();
        builder.Services.AddFlow<FailingFlow>();
        if (registerCronTrigger)
        {
            builder.Services.AddSingleton<ICronTrigger, GreetCronTrigger>();
        }

        if (progressObserver is not null)
        {
            builder.Services.AddSingleton(progressObserver);
        }

        builder.Services.AddSingleton(observer);
        builder.Services.AddHostedService<CapturingFlowBackgroundService>();
        return builder.Build();
    }
}
