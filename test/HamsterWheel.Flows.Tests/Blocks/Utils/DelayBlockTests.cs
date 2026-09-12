using System.Diagnostics;
using FluentAssertions;
using HamsterWheel.Flows.Blocks.Utils;
using HamsterWheel.Flows.Tests.Utils;

namespace HamsterWheel.Flows.Tests.Blocks.Utils;

public class DelayBlockTests
{
    [Fact]
    public async Task WhenDelaySecondsGiven_ThenWaitsThatLongAndLogs()
    {
        //arrange
        var block = new DelayBlock { Id = "delay-block" };
        var pipeline = block.InitBlock();
        var messages = new List<string>();
        pipeline.Logger.SetSink(m => messages.Add(m.Message));
        block.Inputs.Const = 1;

        //act
        var stopwatch = Stopwatch.StartNew();
        await pipeline.Run().WaitSeconds(5);
        stopwatch.Stop();

        //assert
        stopwatch.Elapsed.Should().BeGreaterThan(TimeSpan.FromMilliseconds(900));
        messages.Should().Contain(m => m.Contains("Delaying for 1 second(s)"));
        messages.Should().Contain(m => m.Contains("Delay finished"));
    }

    [Fact]
    public async Task WhenDelayNotLongerThanZero_ThenCompletesWithoutWaiting()
    {
        //arrange
        var block = new DelayBlock { Id = "delay-block" };
        var pipeline = block.InitBlock();
        var messages = new List<string>();
        pipeline.Logger.SetSink(m => messages.Add(m.Message));
        block.Inputs.Const = 0;

        //act
        var stopwatch = Stopwatch.StartNew();
        await pipeline.Run().WaitSeconds(5);
        stopwatch.Stop();

        //assert
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(500));
        messages.Should().Contain(m => m.Contains("nothing to wait for"));
    }
}
