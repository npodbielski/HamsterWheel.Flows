using FluentAssertions;
using HamsterWheel.Flows;
using HamsterWheel.Flows.Runner;

namespace HamsterWheel.Flows.Tests.Infrastructure;

public class CronTriggerBaseTests
{
    [Fact]
    public void WhenUserIdSetOnCronTriggerBase_ThenItIsReturned()
    {
        //arrange
        var trigger = new TestCronTrigger();
        var userId = Guid.NewGuid();

        //act
        trigger.UserId = userId;

        //assert
        trigger.UserId.Should().Be(userId);
    }

    [Fact]
    public void WhenCronTriggerBaseCreated_ThenCronAndFlowNameAreProvidedBySubclass()
    {
        //arrange
        var trigger = new TestCronTrigger();

        //act
        // (properties are read directly)

        //assert
        trigger.Cron.Should().Be("0 0 * * *");
        trigger.FlowName.Should().Be(new FlowName("test"));
    }

    private class TestCronTrigger : CronTriggerBase
    {
        public override string Cron => "0 0 * * *";
        public override FlowName FlowName => new("test");
    }
}
