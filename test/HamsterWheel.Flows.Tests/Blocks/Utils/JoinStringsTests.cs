using FluentAssertions;
using HamsterWheel.Flows.Blocks.Utils;

namespace HamsterWheel.Flows.Tests.Blocks.Utils;

public class JoinStringsTests
{
    [Fact]
    public async Task WhenHaveTwoStrings_ThenReturnsExpectedResult()
    {
        //arrange 
        var sut = new JoinStringsBlock();
        var pipeline = sut.InitBlock();
        sut.Inputs.First.Const = "natan";
        sut.Inputs.Second.Const = "podbielski";
        sut.Inputs.Delimiter.Const = " ";

        //act
        await pipeline.Run();

        //assert
        (await sut.Result.SingleValue).Should().Be("natan podbielski");
    }

    [Fact]
    public async Task WhenHaveThreeStringsAndDelimiter_ThenReturnsExpectedResult()
    {
        //arrange 
        var sut = new JoinStringsBlock();
        var pipeline = sut.InitBlock();
        sut.Inputs.First.Const = "1";
        sut.Inputs.Second.Const = "2";
        sut.Inputs.Third.Const = "3";
        sut.Inputs.Delimiter.Const = ",";

        //act
        await pipeline.Run();

        //assert
        (await sut.Result.SingleValue).Should().Be("1,2,3");
    }

    [Fact]
    public async Task WhenHaveIEnumerableStrings_ThenReturnsExpectedResult()
    {
        //arrange 
        var sut = new JoinStringsBlock();
        var pipeline = sut.InitBlock();
        sut.Inputs.Strings.Const = ["a","b","c"];
        sut.Inputs.Delimiter.Const = ",";

        //act
        await pipeline.Run();

        //assert
        (await sut.Result.SingleValue).Should().Be("a,b,c");
    }
}