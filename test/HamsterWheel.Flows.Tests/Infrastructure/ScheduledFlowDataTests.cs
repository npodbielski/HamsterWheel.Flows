using FluentAssertions;
using HamsterWheel.Flows;
using HamsterWheel.Flows.Runner;
using NSubstitute;

namespace HamsterWheel.Flows.Tests.Infrastructure;

public class ScheduledFlowDataTests
{
    [Fact]
    public void WhenCreateNew_ThenPropertiesAreSet()
    {
        //arrange
        var flowName = new FlowName("my-flow");
        var input = new { Value = 1 };

        //act
        var data = ScheduledFlowData.New(flowName, "user-1", input);

        //assert
        data.FlowName.Should().Be(flowName);
        data.UserId.Should().Be("user-1");
        data.Input.Should().BeSameAs(input);
    }

    [Fact]
    public void WhenValidateWithAllFields_ThenNoException()
    {
        //arrange
        var data = Substitute.For<IScheduledFlowData>();
        data.ScheduledId.Returns(Guid.NewGuid());
        data.FlowName.Returns(new FlowName("test"));

        //act
        var action = () => data.Validate();

        //assert
        action.Should().NotThrow();
    }

    [Fact]
    public void WhenValidateOnRecordData_ThenNoException()
    {
        //arrange
        var data = ScheduledFlowData.New(new FlowName("my-flow"), "user-1", new { Value = 1 });

        //act
        var action = () => data.Validate();

        //assert
        action.Should().NotThrow();
    }

    [Fact]
    public void WhenCreateNewSubFlow_ThenIsSubFlowIsTrueAndValidationPasses()
    {
        //arrange
        var data = ScheduledFlowData.NewSubFlow(new FlowName("my-flow"), null, null);

        //act
        var action = () => data.Validate();

        //assert
        data.IsSubFlow.Should().BeTrue();
        action.Should().NotThrow();
    }
}
