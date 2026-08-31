using FluentAssertions;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Logic;
using HamsterWheel.Flows.Tests.Utils;

namespace HamsterWheel.Flows.Tests.Blocks;

public class TakeFirstBlockTests
{
    [Fact]
    public async Task WhenInputIsEnumerable_ThenReturnsFirstElement()
    {
        //arrange
        var block = new TakeFirstBlock();
        var pipeline = block.InitBlock();
        block.Inputs.Const = new[] { "first", "second", "third" };

        //act
        await pipeline.Run().WaitSeconds(5);
        var actual = await block.Result.SingleValue.WaitSeconds(5);

        //assert
        actual.Should().Be("first");
    }

    [Fact]
    public async Task WhenInputIsNotEnumerable_ThenReturnsInputItself()
    {
        //arrange
        var block = new TakeFirstBlock();
        var pipeline = block.InitBlock();
        block.Inputs.Const = (object)42; // int is not IEnumerable

        //act
        await pipeline.Run().WaitSeconds(5);
        var actual = await block.Result.SingleValue.WaitSeconds(5);

        //assert
        actual.Should().Be(42);
    }

    [Fact]
    public async Task WhenInputIsSingleValueEnumerable_ThenReturnsThatValue()
    {
        //arrange
        var block = new TakeFirstBlock();
        var pipeline = block.InitBlock();
        block.Inputs.Const = new[] { 42 };

        //act
        await pipeline.Run().WaitSeconds(5);
        var actual = await block.Result.SingleValue.WaitSeconds(5);

        //assert
        actual.Should().Be(42);
    }
}
