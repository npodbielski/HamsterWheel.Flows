using System.Threading.Channels;
using FluentAssertions;
using HamsterWheel.Flows;
using HamsterWheel.Flows.Api;
using HamsterWheel.Flows.Runner;
using HamsterWheel.Flows.Setup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace HamsterWheel.Flows.Tests.Api;

/// <summary>
/// Starter logic against a real channel + FlowBackgroundService
/// </summary>
public class ChannelFlowRunStarterTests
{
    [Fact]
    public async Task WhenRunSucceeds_ThenOutcomeIsSuccessWithOutput()
    {
        //arrange
        await using var host = await CreateStarterHost();

        //act
        var outcome = await host.Starter.RunAsync(new FlowName(nameof(StarterSuccessFlow)), "user-1", null,
            TimeSpan.FromSeconds(10), CancellationToken.None);

        //assert
        outcome.Status.Should().Be(FlowRunStatus.Success);
        outcome.Error.Should().BeNull();
        outcome.RunId.Should().NotBeEmpty();
        outcome.Output.Should().BeOfType<StarterSuccessFlowOutput>()
            .Which.Value.Should().Be("hello-from-starter");
    }

    [Fact]
    public async Task WhenRunFails_ThenOutcomeIsFailedWithError()
    {
        //arrange
        await using var host = await CreateStarterHost();

        //act
        var outcome = await host.Starter.RunAsync(new FlowName(nameof(StarterFailingFlow)), "user-1", null,
            TimeSpan.FromSeconds(10), CancellationToken.None);

        //assert
        outcome.Status.Should().Be(FlowRunStatus.Failed);
        outcome.Output.Should().BeNull();
        outcome.Error.Should().NotBeNull();
        outcome.Error!.ToString().Should().Contain("starter test failure");
    }

    [Fact]
    public async Task WhenFlowNotFound_ThenOutcomeIsFailedWithFlowNotFoundException()
    {
        //arrange
        await using var host = await CreateStarterHost();

        //act
        var outcome = await host.Starter.RunAsync(new FlowName("MissingFlow"), null, null,
            TimeSpan.FromSeconds(10), CancellationToken.None);

        //assert
        outcome.Status.Should().Be(FlowRunStatus.Failed);
        outcome.Error.Should().BeOfType<FlowNotFoundException>();
    }

    [Fact]
    public async Task WhenRunExceedsTimeout_ThenOutcomeIsTimedOut()
    {
        //arrange (StarterHangingFlow runs until its 300ms max run time; the starter budget is 50ms)
        await using var host = await CreateStarterHost();

        //act
        var outcome = await host.Starter.RunAsync(new FlowName(nameof(StarterHangingFlow)), "user-1", null,
            TimeSpan.FromMilliseconds(50), CancellationToken.None);

        //assert
        outcome.Status.Should().Be(FlowRunStatus.TimedOut);
        outcome.Output.Should().BeNull();
        outcome.Error.Should().BeNull();
        outcome.RunId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task WhenNoFlowRunnerRegistered_ThenOutcomeIsFailedWithNoFlowRunnerException()
    {
        //arrange (no IFlowRunner in the service provider)
        var services = new ServiceCollection()
            .AddLogging();
        var channel = Channel.CreateUnbounded<IScheduledFlowData>();
        var provider = services
            .AddSingleton(Substitute.For<IFlowScheduler>())
            .AddSingleton(channel)
            .BuildServiceProvider();
        var service = new FlowBackgroundService(provider.GetRequiredService<IFlowScheduler>(), provider, channel,
            Substitute.For<ILogger<FlowBackgroundService>>());
        await service.StartAsync(CancellationToken.None);

        //act
        var outcome = await new ChannelFlowRunStarter(channel).RunAsync(new FlowName("any-flow"), "user-1", null,
            TimeSpan.FromSeconds(5), CancellationToken.None);

        //assert
        outcome.Status.Should().Be(FlowRunStatus.Failed);
        outcome.Error.Should().BeOfType<NoFlowRunnerException>();
        await StopService(service);
    }

    private static async Task<StarterHost> CreateStarterHost()
    {
        var services = new ServiceCollection()
            .AddFlowsWithDependencies()
            .AddLogging();
        services.AddFlow<StarterSuccessFlow>();
        services.AddFlow<StarterFailingFlow>();
        services.AddFlow<StarterHangingFlow>();
        var provider = services.BuildServiceProvider();
        var channel = provider.GetRequiredService<Channel<IScheduledFlowData>>();
        var scheduler = provider.GetRequiredService<IFlowScheduler>();
        var service = new FlowBackgroundService(scheduler, provider, channel,
            Substitute.For<ILogger<FlowBackgroundService>>());
        await service.StartAsync(CancellationToken.None);
        return new StarterHost(new ChannelFlowRunStarter(channel), provider, service);
    }

    private static async Task StopService(FlowBackgroundService service)
    {
        try
        {
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            //expected: channel read is cancelled on shutdown
        }
    }

    private sealed class StarterHost(ChannelFlowRunStarter starter, ServiceProvider provider,
        FlowBackgroundService service) : IAsyncDisposable
    {
        public ChannelFlowRunStarter Starter { get; } = starter;

        public async ValueTask DisposeAsync()
        {
            await StopService(service);
            provider.Dispose();
        }
    }
}
