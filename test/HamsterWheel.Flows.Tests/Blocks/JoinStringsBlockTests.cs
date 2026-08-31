using FluentAssertions;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Utils;
using HamsterWheel.Flows.Tests.Utils;

namespace HamsterWheel.Flows.Tests.Blocks;

public class JoinStringsBlockTests
{
    [Fact]
    public async Task WhenTwoInputsJoinedWithDelimiter_ThenReturnsJoinedString()
    {
        //arrange
        var block = new JoinStringsBlock();
        var pipeline = block.InitBlock();
        block.Inputs.First.Const = "Hello";
        block.Inputs.Second.Const = "World";
        block.Inputs.Delimiter.Const = " ";

        //act
        await pipeline.Run().WaitSeconds(5);
        var actual = await block.Result.SingleValue.WaitSeconds(5);

        //assert
        actual.Should().Be("Hello World");
    }

    [Fact]
    public async Task WhenNoDelimiter_ThenStringsAreConcatenated()
    {
        //arrange
        var block = new JoinStringsBlock();
        var pipeline = block.InitBlock();
        block.Inputs.First.Const = "foo";
        block.Inputs.Second.Const = "bar";

        //act
        await pipeline.Run().WaitSeconds(5);
        var actual = await block.Result.SingleValue.WaitSeconds(5);

        //assert
        actual.Should().Be("foobar");
    }

    [Fact]
    public async Task WhenEmptyDelimiter_ThenStringsAreConcatenated()
    {
        //arrange
        var block = new JoinStringsBlock();
        var pipeline = block.InitBlock();
        block.Inputs.First.Const = "a";
        block.Inputs.Second.Const = "b";
        block.Inputs.Delimiter.Const = "";

        //act
        await pipeline.Run().WaitSeconds(5);
        var actual = await block.Result.SingleValue.WaitSeconds(5);

        //assert
        actual.Should().Be("ab");
    }
}
