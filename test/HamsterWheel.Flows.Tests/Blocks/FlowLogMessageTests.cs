using FluentAssertions;
using HamsterWheel.Flows.Monitoring;

namespace HamsterWheel.Flows.Tests.Blocks;

public class FlowLogMessageTests
{
    [Fact]
    public void WhenFlowLogMessageWithBlockId_ThenBlockIdInProperties()
    {
        //arrange
        var msg = new FlowLogMessage(FlowLogLevel.Info, DateTimeOffset.UtcNow, "test", "block-1", "IterateBlock");

        //act & assert
        msg.BlockId.Should().Be("block-1");
        msg.BlockType.Should().Be("IterateBlock");
        msg.Properties.Should().ContainKey("blockId");
        msg.Properties.Should().ContainKey("blockType");
    }

    [Fact]
    public void WhenFlowLogMessageWithoutBlockId_ThenNull()
    {
        //arrange
        var msg = new FlowLogMessage(FlowLogLevel.Info, DateTimeOffset.UtcNow, "test");

        //act & assert
        msg.BlockId.Should().BeNull();
        msg.BlockType.Should().BeNull();
    }

    [Fact]
    public void WhenFlowLogMessageWithProperties_ThenKept()
    {
        //arrange
        var props = new Dictionary<string, object> { ["custom"] = "value" };
        var msg = new FlowLogMessage(FlowLogLevel.Warning, DateTimeOffset.UtcNow, "msg", Properties: props);

        //act & assert
        msg.Properties.Should().Contain("custom", "value");
    }

    [Fact]
    public void WhenFlowLogMessageDeconstruct_ThenReturnsAllParts()
    {
        //arrange
        var msg = new FlowLogMessage(FlowLogLevel.Error, DateTimeOffset.UtcNow, "error-msg", "bid", "btype");

        //act
        var (level, dateTime, message, properties) = msg;

        //assert
        level.Should().Be(FlowLogLevel.Error);
        message.Should().Be("error-msg");
        properties.Should().BeSameAs(msg.Properties);
    }
}
