using FluentAssertions;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Logic;

namespace HamsterWheel.Flows.Tests.Infrastructure;

public class BlockExtensionsTests
{
    [Fact]
    public void WhenIfWithTrue_ThenConditionIsTrue()
    {
        //arrange
        var block = new IterateBlock();

        //act
        block.If(true);

        //assert
        block.Condition.Should().NotBeNull();
        block.Condition!.Result.Should().BeTrue();
    }

    [Fact]
    public void WhenIfWithFalse_ThenConditionIsFalse()
    {
        //arrange
        var block = new IterateBlock();

        //act
        block.If(false);

        //assert
        block.Condition.Should().NotBeNull();
        block.Condition!.Result.Should().BeFalse();
    }

    [Fact]
    public void WhenIfWithNull_ThenConditionIsFalse()
    {
        //arrange
        var block = new IterateBlock();

        //act
        block.If(null);

        //assert
        block.Condition.Should().NotBeNull();
        block.Condition!.Result.Should().BeFalse();
    }

    [Fact]
    public void WhenIfWithInteger_ThenConvertedToBoolean()
    {
        //arrange
        var block = new IterateBlock();

        //act
        block.If(1);

        //assert
        block.Condition.Should().NotBeNull();
        block.Condition!.Result.Should().BeTrue();
    }
}
