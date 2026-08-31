using System.Collections.Concurrent;
using FluentAssertions;
using HamsterWheel.Flows.Monitoring;

namespace HamsterWheel.Flows.Tests.Infrastructure;

public class PipelineLoggerTests
{
    [Fact]
    public void WhenLogWithString_ThenSinkReceivesMessage()
    {
        //arrange
        var logger = new PipelineLogger();
        var messages = new ConcurrentQueue<IFlowLogMessage>();
        logger.SetSink(m => messages.Enqueue(m));

        //act
        logger.Log("hello world");

        //assert
        messages.Should().ContainSingle();
        var msg = messages.TryDequeue(out var m) ? m : null;
        msg!.Message.Should().Be("hello world");
        msg.Level.Should().Be(FlowLogLevel.Info);
    }

    [Fact]
    public void WhenLogWithFlowLogMessage_ThenSinkReceivesIt()
    {
        //arrange
        var logger = new PipelineLogger();
        var messages = new ConcurrentQueue<IFlowLogMessage>();
        logger.SetSink(m => messages.Enqueue(m));

        //act
        logger.Log(new FlowLogMessage(FlowLogLevel.Warning, DateTimeOffset.UtcNow, "warn msg", "block-1", "TestBlock"));

        //assert
        messages.Should().ContainSingle();
        messages.TryDequeue(out var m);
        m!.Message.Should().Be("warn msg");
        m.Level.Should().Be(FlowLogLevel.Warning);
        m.BlockId.Should().Be("block-1");
        m.BlockType.Should().Be("TestBlock");
    }

    [Fact]
    public void WhenMultipleSinksSet_ThenAllReceiveMessages()
    {
        //arrange
        var logger = new PipelineLogger();
        var sink1 = new ConcurrentQueue<string>();
        var sink2 = new ConcurrentQueue<string>();
        logger.SetSink(m => sink1.Enqueue(m.Message));
        logger.SetSink(m => sink2.Enqueue(m.Message));

        //act
        logger.Log("test");

        //assert
        sink1.Should().ContainSingle().Which.Should().Be("test");
        sink2.Should().ContainSingle().Which.Should().Be("test");
    }
}
