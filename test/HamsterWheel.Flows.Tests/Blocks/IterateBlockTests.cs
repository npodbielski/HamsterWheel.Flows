using FluentAssertions;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Logic;
using HamsterWheel.Flows.Tests.Utils;

namespace HamsterWheel.Flows.Tests.Blocks;

public class IterateBlockTests
{
    [Fact]
    public async Task WhenInputIsEnumerable_ThenEachItemIsPushedAsOutput()
    {
        //arrange
        var block = new IterateBlock();
        var pipeline = block.InitBlock();
        block.Inputs.Const = new[] { "a", "b", "c" };

        //act
        var enumerator = block.Result.AsEnumerable();
        await pipeline.Run().WaitSeconds(5);
        var actual = await enumerator.ToArrayAsync().AsTask().WaitSeconds(5);

        //assert
        actual.Should().BeEquivalentTo(["a", "b", "c"]);
    }

    [Fact]
    public async Task WhenInputIsNotEnumerable_ThenNoOutput()
    {
        //arrange
        var block = new IterateBlock();
        var pipeline = block.InitBlock();
        block.Inputs.Const = (object)42; // int is not IEnumerable

        //act
        var enumerator = block.Result.AsEnumerable();
        await pipeline.Run().WaitSeconds(5);
        var actual = await enumerator.ToArrayAsync().AsTask().WaitSeconds(5);

        //assert
        actual.Should().BeEmpty();
    }
}
