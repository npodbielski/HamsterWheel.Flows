using FluentAssertions;
using HamsterWheel.Flows;
using HamsterWheel.Flows.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Threading.Channels;

namespace HamsterWheel.Flows.Tests.Infrastructure;

public class FlowSchedulerTests
{
    [Fact]
    public async Task WhenScheduleWithDateTimeOffset_ThenChannelReceivesFlowDataAfterTimerFires()
    {
        //arrange
        var (scheduler, channel) = CreateScheduler();
        var userId = Guid.NewGuid();
        var at = DateTimeOffset.UtcNow.AddMilliseconds(150);

        //act
        scheduler.Schedule(new FlowName("test-flow"), userId, at, "flow-input");

        //assert
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var data = await channel.Reader.ReadAsync(cts.Token);
        data.FlowName.Should().Be(new FlowName("test-flow"));
        data.UserId.Should().Be(userId.ToString());
        data.Input.Should().Be("flow-input");
    }

    [Fact]
    public async Task WhenScheduleWithSecondsCron_ThenChannelReceivesFlowDataAfterTimerFires()
    {
        //arrange
        var (scheduler, channel) = CreateScheduler();

        //act
        scheduler.Schedule(new FlowName("test-flow"), Guid.NewGuid(), "*/1 * * * * *");

        //assert
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var data = await channel.Reader.ReadAsync(cts.Token);
        data.FlowName.Should().Be(new FlowName("test-flow"));
        scheduler.CancelAll();
    }

    [Fact]
    public void WhenCronExpressionHasSeconds_ThenItIsRecognized()
    {
        //arrange
        var withSeconds = "*/5 * * * * *";
        var standard = "* * * * *";
        var invalid = "not-a-cron";

        //act + assert
        withSeconds.IsCronExpression().Should().BeTrue();
        standard.IsCronExpression().Should().BeTrue();
        invalid.IsCronExpression().Should().BeFalse();
        withSeconds.ParseCronExpression().Should().NotBeNull();
    }

    [Fact]
    public async Task WhenScheduleWithValidCron_ThenDoesNotThrow()
    {
        //arrange
        var (scheduler, channel) = CreateScheduler();

        //act
        var action = () => scheduler.Schedule(new FlowName("test-flow"), Guid.NewGuid(), "* * * * *");

        //assert
        action.Should().NotThrow();
        scheduler.CancelAll();
    }

    [Fact]
    public async Task WhenScheduleWithInvalidCron_ThenDoesNotThrowAndNothingIsWritten()
    {
        //arrange
        var (scheduler, channel) = CreateScheduler();

        //act
        var action = () => scheduler.Schedule(new FlowName("test-flow"), Guid.NewGuid(), "not-a-cron");
        await Task.Delay(200);

        //assert
        action.Should().NotThrow();
        channel.Reader.TryRead(out _).Should().BeFalse();
    }

    [Fact]
    public async Task WhenCancelFlow_ThenCancelledFlowStopsAndOthersStillFire()
    {
        //arrange
        var (scheduler, channel) = CreateScheduler();
        var now = DateTimeOffset.UtcNow;
        scheduler.Schedule(new FlowName("flow-a"), Guid.NewGuid(), now.AddMilliseconds(150));
        scheduler.Schedule(new FlowName("flow-b"), Guid.NewGuid(), now.AddMilliseconds(400));
        scheduler.Cancel(new FlowName("flow-a"));

        //act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var data = await channel.Reader.ReadAsync(cts.Token);

        //assert
        data.FlowName.Should().Be(new FlowName("flow-b"));
        await Task.Delay(300);
        channel.Reader.TryRead(out _).Should().BeFalse();
    }

    [Fact]
    public async Task WhenCancelAll_ThenNoFlowsFire()
    {
        //arrange
        var (scheduler, channel) = CreateScheduler();
        var now = DateTimeOffset.UtcNow;
        scheduler.Schedule(new FlowName("flow-a"), Guid.NewGuid(), now.AddMilliseconds(150));
        scheduler.Schedule(new FlowName("flow-b"), Guid.NewGuid(), now.AddMilliseconds(300));

        //act
        scheduler.CancelAll();
        await Task.Delay(500);

        //assert
        channel.Reader.TryRead(out _).Should().BeFalse();
    }

    [Fact]
    public async Task WhenCancelWithoutTimers_ThenDoesNotThrow()
    {
        //arrange
        var (scheduler, channel) = CreateScheduler();

        //act
        var action = () => scheduler.Cancel(new FlowName("unknown-flow"));

        //assert
        action.Should().NotThrow();
    }

    [Fact]
    public async Task WhenLoadTriggers_ThenAllCronTriggersFromDiAreScheduled()
    {
        //arrange
        var services = new ServiceCollection()
            .AddSingleton<ICronTrigger>(new TestCronTrigger("trigger-1"))
            .AddSingleton<ICronTrigger>(new TestCronTrigger("trigger-2"))
            .BuildServiceProvider();
        var channel = Channel.CreateUnbounded<IScheduledFlowData>();
        var scheduler = new RecordingFlowScheduler(services, channel, Substitute.For<ILogger<IFlowScheduler>>());

        //act
        await scheduler.LoadTriggers();

        //assert
        scheduler.Scheduled.Should().HaveCount(2);
        scheduler.Scheduled.Select(s => s.Name)
            .Should().BeEquivalentTo(new[] { new FlowName("trigger-1"), new FlowName("trigger-2") });
    }

    [Fact]
    public async Task WhenLoadTriggersSecondCall_ThenIsNoOp()
    {
        //arrange
        var services = new ServiceCollection()
            .AddSingleton<ICronTrigger>(new TestCronTrigger("trigger-1"))
            .BuildServiceProvider();
        var channel = Channel.CreateUnbounded<IScheduledFlowData>();
        var scheduler = new RecordingFlowScheduler(services, channel, Substitute.For<ILogger<IFlowScheduler>>());

        //act
        await scheduler.LoadTriggers();
        await scheduler.LoadTriggers();

        //assert
        scheduler.Scheduled.Should().HaveCount(1);
    }

    private static (FlowScheduler Scheduler, Channel<IScheduledFlowData> Channel) CreateScheduler()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var channel = Channel.CreateUnbounded<IScheduledFlowData>();
        var logger = Substitute.For<ILogger<IFlowScheduler>>();
        return (new FlowScheduler(services, channel, logger), channel);
    }

    private class TestCronTrigger(string name) : CronTriggerBase
    {
        public override string Cron => "0 0 * * *";
        public override FlowName FlowName => new(name);
    }

    private class RecordingFlowScheduler(IServiceProvider services, Channel<IScheduledFlowData> channel,
        ILogger<IFlowScheduler> logger) : FlowScheduler(services, channel, logger)
    {
        public List<(FlowName Name, string Cron)> Scheduled { get; } = [];

        public override void Schedule(FlowName flowName, Guid userId, string scheduleExpression, object? input = null)
            => Scheduled.Add((flowName, scheduleExpression));
    }
}
