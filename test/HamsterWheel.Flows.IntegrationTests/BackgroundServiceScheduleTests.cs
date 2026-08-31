using FluentAssertions;
using HamsterWheel.Flows;
using HamsterWheel.Flows.IntegrationTests.Fixtures;
using HamsterWheel.Flows.IntegrationTests.Flows;
using HamsterWheel.Flows.IntegrationTests.Observing;
using HamsterWheel.Flows.Runner;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.Flows.IntegrationTests;

public class BackgroundServiceScheduleTests
{
    [Fact]
    public async Task WhenFlowScheduledAtDateTime_ThenBackgroundServiceRunsFlow()
    {
        //arrange
        var observer = new FlowRunObserver();
        var host = FlowHostBuilder.Build(observer, registerCronTrigger: false);
        await host.StartAsync();
        var scheduler = host.Services.GetRequiredService<IFlowScheduler>();

        //act
        scheduler.Schedule(new FlowName(nameof(GreetFlow)), Guid.NewGuid(), DateTimeOffset.UtcNow.AddSeconds(1));
        var run = await observer.WaitNextCompletedAsync(TimeSpan.FromSeconds(15));
        await host.StopAsync();

        //assert
        run.Message.FlowName.Name.Should().Be(nameof(GreetFlow));
        var output = run.Result.Output.Should().BeOfType<GreetFlowOutput>().Subject;
        output.Greeting.Should().Be("hello world");
    }

    [Fact]
    public async Task WhenScheduledFlowFails_ThenErrorIsObserved()
    {
        //arrange
        var observer = new FlowRunObserver();
        var host = FlowHostBuilder.Build(observer, registerCronTrigger: false);
        await host.StartAsync();
        var scheduler = host.Services.GetRequiredService<IFlowScheduler>();

        //act
        scheduler.Schedule(new FlowName(nameof(FailingFlow)), Guid.NewGuid(), DateTimeOffset.UtcNow.AddSeconds(1));
        var failure = await observer.WaitNextErrorAsync(TimeSpan.FromSeconds(15));
        await host.StopAsync();

        //assert
        failure.Message.FlowName.Name.Should().Be(nameof(FailingFlow));
        failure.Error.ToString().Should().Contain("boom");
    }
}
