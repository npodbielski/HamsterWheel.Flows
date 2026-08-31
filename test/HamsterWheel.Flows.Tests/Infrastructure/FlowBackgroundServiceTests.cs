using FluentAssertions;
using HamsterWheel.Flows;
using HamsterWheel.Flows.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Threading.Channels;

namespace HamsterWheel.Flows.Tests.Infrastructure;

public class FlowBackgroundServiceTests
{
    [Fact]
    public async Task WhenFlowWrittenToChannel_ThenItIsRun()
    {
        //arrange
        var (service, runner, channel) = CreateService();
        using var cts = new CancellationTokenSource();
        var runTask = service.TestExecuteAsync(cts.Token);
        var message = ScheduledFlowData.New(new FlowName("flow-1"), "user-1", "input-1");

        //act
        await channel.Writer.WriteAsync(message);
        await WaitUntil(() => service.Completed.Count == 1);

        //assert
        runner.Received(1).RunAsync(Arg.Any<IScheduledFlowData>(), Arg.Any<CancellationToken>());
        service.Completed[0].Message.Should().BeSameAs(message);
        service.Completed[0].Result.FlowName.Should().Be(new FlowName("flow-1"));
        await StopService(runTask, cts);
    }

    [Fact]
    public async Task WhenRunAsyncThrows_ThenHandleFlowErrorIsCalled()
    {
        //arrange
        var (service, runner, channel) = CreateService();
        runner.RunAsync(Arg.Any<IScheduledFlowData>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IFlowRunResult>(new InvalidOperationException("run failed")));
        using var cts = new CancellationTokenSource();
        var runTask = service.TestExecuteAsync(cts.Token);

        //act
        await channel.Writer.WriteAsync(ScheduledFlowData.New(new FlowName("flow-1"), "user-1", null));
        await WaitUntil(() => service.Errors.Count == 1);

        //assert
        service.Errors[0].Error.Should().BeOfType<InvalidOperationException>()
            .Which.Message.Should().Contain("run failed");
        service.Completed.Should().BeEmpty();
        await StopService(runTask, cts);
    }

    [Fact]
    public async Task WhenCancelled_ThenExecuteAsyncThrowsAfterRunningFlowsComplete()
    {
        //arrange
        var (service, runner, channel) = CreateService();
        using var cts = new CancellationTokenSource();
        var runTask = service.TestExecuteAsync(cts.Token);
        await channel.Writer.WriteAsync(ScheduledFlowData.New(new FlowName("flow-1"), "user-1", null));
        await WaitUntil(() => service.Completed.Count == 1);

        //act
        cts.Cancel();

        //assert
        runTask.IsCanceled.Should().BeFalse();
        service.Completed.Should().HaveCount(1);
        await StopService(runTask, cts);
    }

    [Fact]
    public async Task WhenTwoFlowsWrittenToChannel_ThenBothAreRun()
    {
        //arrange
        var (service, runner, channel) = CreateService();
        using var cts = new CancellationTokenSource();
        var runTask = service.TestExecuteAsync(cts.Token);
        var message1 = ScheduledFlowData.New(new FlowName("flow-1"), "user-1", null);
        var message2 = ScheduledFlowData.New(new FlowName("flow-2"), "user-2", null);

        //act
        await channel.Writer.WriteAsync(message1);
        await channel.Writer.WriteAsync(message2);
        await WaitUntil(() => service.Completed.Count == 2);

        //assert (messages are pushed to a stack, so run order is not write order)
        runner.Received(2).RunAsync(Arg.Any<IScheduledFlowData>(), Arg.Any<CancellationToken>());
        service.Completed.Select(c => c.Message).Should().Contain(new[] { message1, message2 });
        await StopService(runTask, cts);
    }

    [Fact]
    public async Task WhenScheduleFlowDirect_ThenFlowIsRun()
    {
        //arrange
        var (service, runner, channel) = CreateService();
        using var cts = new CancellationTokenSource();
        var runTask = service.TestExecuteAsync(cts.Token);
        var message = ScheduledFlowData.New(new FlowName("direct-flow"), "user-1", "direct-input");

        //act
        await service.TestScheduleFlowDirect(message);
        await WaitUntil(() => service.Completed.Count == 1);

        //assert
        runner.Received(1).RunAsync(Arg.Any<IScheduledFlowData>(), Arg.Any<CancellationToken>());
        service.Completed[0].Message.Should().BeSameAs(message);
        await StopService(runTask, cts);
    }

    private static async Task WaitUntil(Func<bool> condition, int timeoutMs = 2000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
        }
    }

    private static async Task StopService(Task runTask, CancellationTokenSource cts)
    {
        cts.Cancel();
        try
        {
            await runTask;
        }
        catch (OperationCanceledException)
        {
            //expected: ExecuteAsync propagates channel read cancellation
        }
    }

    private static (TestFlowBackgroundService Service, IFlowRunner Runner, Channel<IScheduledFlowData> Channel)
        CreateService()
    {
        var runner = Substitute.For<IFlowRunner>();
        runner.RunAsync(Arg.Any<IScheduledFlowData>(), Arg.Any<CancellationToken>())
            .Returns(new FlowRunResult(new FlowName("flow-1"), "output-1"));
        var scheduler = Substitute.For<IFlowScheduler>();
        var services = new ServiceCollection()
            .AddSingleton(runner)
            .BuildServiceProvider();
        var channel = Channel.CreateUnbounded<IScheduledFlowData>();
        var logger = Substitute.For<ILogger<FlowBackgroundService>>();
        return (new TestFlowBackgroundService(scheduler, services, channel, logger), runner, channel);
    }

    private class TestFlowBackgroundService(
        IFlowScheduler scheduler,
        IServiceProvider services,
        Channel<IScheduledFlowData> channel,
        ILogger<FlowBackgroundService> logger)
        : FlowBackgroundService(scheduler, services, channel, logger)
    {
        public List<IScheduledFlowData> Started { get; } = [];
        public List<(IScheduledFlowData Message, IFlowRunResult Result)> Completed { get; } = [];
        public List<(IScheduledFlowData Message, Exception Error)> Errors { get; } = [];

        public Task TestExecuteAsync(CancellationToken token) => ExecuteAsync(token);
        public Task TestScheduleFlowDirect(IScheduledFlowData message) => ScheduleFlowDirect(message);

        protected override Task HandleFlowStarted(IScheduledFlowData message)
        {
            Started.Add(message);
            return Task.CompletedTask;
        }

        protected override Task HandleFlowCompleted(IScheduledFlowData message, IFlowRunResult result)
        {
            Completed.Add((message, result));
            return Task.CompletedTask;
        }

        protected override Task HandleFlowError(IScheduledFlowData message, Exception e)
        {
            Errors.Add((message, e));
            return Task.CompletedTask;
        }
    }
}
