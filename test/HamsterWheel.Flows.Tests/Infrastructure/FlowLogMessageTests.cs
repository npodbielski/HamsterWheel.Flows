using FluentAssertions;
using HamsterWheel.Flows.Monitoring;

namespace HamsterWheel.Flows.Tests.Infrastructure;

public class FlowLogMessageTests
{
    [Fact]
    public void WhenCreatedWithBlockInfo_ThenBlockIdAndTypeAreSet()
    {
        //arrange
        // (none needed)

        //act
        var msg = new FlowLogMessage(FlowLogLevel.Info, DateTimeOffset.UtcNow, "test", "id-1", "MyBlock");

        //assert
        msg.BlockId.Should().Be("id-1");
        msg.BlockType.Should().Be("MyBlock");
        msg.Message.Should().Be("test");
        msg.Level.Should().Be(FlowLogLevel.Info);
    }

    [Fact]
    public void WhenCreatedWithoutBlockInfo_ThenBlockIdAndTypeAreNull()
    {
        //arrange
        // (none needed)

        //act
        var msg = new FlowLogMessage(FlowLogLevel.Error, DateTimeOffset.UtcNow, "error");

        //assert
        msg.BlockId.Should().BeNull();
        msg.BlockType.Should().BeNull();
        msg.Level.Should().Be(FlowLogLevel.Error);
    }

    [Fact]
    public void WhenDeconstructed_ThenAllFieldsAreReturned()
    {
        //arrange
        var msg = new FlowLogMessage(FlowLogLevel.Info, DateTimeOffset.UtcNow, "msg", "id", "type");

        //act
        var (level, dateTime, message, properties) = msg;

        //assert
        level.Should().Be(FlowLogLevel.Info);
        message.Should().Be("msg");
        properties.Should().NotBeNull();
    }

    [Fact]
    public void WhenCreatedWithProperties_ThenPropertiesAreIncluded()
    {
        //arrange
        var props = new Dictionary<string, object> { ["custom"] = "value" };

        //act
        var msg = new FlowLogMessage(FlowLogLevel.Info, DateTimeOffset.UtcNow, "test", Properties: props);

        //assert
        msg.Properties.Should().ContainKey("custom").WhoseValue.Should().Be("value");
    }
}
