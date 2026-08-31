using FluentAssertions;
using HamsterWheel.Flows.IntegrationTests.Fixtures;
using HamsterWheel.Flows.IntegrationTests.Flows;
using HamsterWheel.Flows.IntegrationTests.Observing;

namespace HamsterWheel.Flows.IntegrationTests;

public class BackgroundServiceCronTests
{
    [Fact]
    public async Task WhenCronTriggerFires_ThenBackgroundServiceRunsFlow()
    {
        //arrange
        var observer = new FlowRunObserver();
        var host = FlowHostBuilder.Build(observer);
        await host.StartAsync();

        //act
        //the every-5-seconds cron trigger fires at the next 5s boundary
        var run = await observer.WaitNextCompletedAsync(TimeSpan.FromSeconds(30));
        await host.StopAsync();

        //assert
        run.Message.FlowName.Name.Should().Be(nameof(GreetFlow));
        run.Result.FlowName.Name.Should().Be(nameof(GreetFlow));
        var output = run.Result.Output.Should().BeOfType<GreetFlowOutput>().Subject;
        output.Greeting.Should().Be("hello world");
    }
}
