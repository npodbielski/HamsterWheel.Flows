using FluentAssertions;
using HamsterWheel.Flows.IntegrationTests.Fixtures;
using HamsterWheel.Flows.IntegrationTests.Flows;
using HamsterWheel.Flows.IntegrationTests.Observing;

namespace HamsterWheel.Flows.IntegrationTests;

public class FlowProgressObserverIntegrationTests
{
    [Fact]
    public async Task WhenCronTriggerFires_ThenFlowProgressIsObservedEndToEnd()
    {
        //arrange
        var runObserver = new FlowRunObserver();
        var progressObserver = new FlowProgressObserverCapture();
        var host = FlowHostBuilder.Build(runObserver, progressObserver: progressObserver);
        await host.StartAsync();

        //act (the every-5-seconds cron trigger fires at the next 5s boundary)
        var run = await runObserver.WaitNextCompletedAsync(TimeSpan.FromSeconds(30));
        await progressObserver.WaitFlowFinishedAsync(TimeSpan.FromSeconds(30));
        await host.StopAsync();

        //assert
        progressObserver.RunId.Should().Be(run.Message.ScheduledId);
        progressObserver.FlowName.Should().NotBeNull();
        progressObserver.FlowName!.Name.Should().Be(nameof(GreetFlow));
        //GreetFlow has two blocks: join strings and set output
        progressObserver.BlockCount.Should().Be(2);
        progressObserver.Events.Should().ContainInOrder("FlowStarted", "FlowCompleted");
        progressObserver.Events.Count(e => e.StartsWith("BlockCompleted:")).Should().Be(2);
        progressObserver.Events.Should().NotContain(e => e.StartsWith("BlockFailed:"));
        var output = progressObserver.Output.Should().BeOfType<GreetFlowOutput>().Subject;
        output.Greeting.Should().Be("hello world");
    }
}
