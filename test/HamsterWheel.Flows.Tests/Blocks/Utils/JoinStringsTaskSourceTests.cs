using FluentAssertions;
using HamsterWheel.Flows.Blocks.Utils;

namespace HamsterWheel.Flows.Tests.Blocks.Utils;

public class JoinStringsTaskSourceTests
{
    [Fact]
    public async Task WhenStringsConst_GetYieldsChunksFromStrings()
    {
        //arrange
        var sut = new JoinStringsTaskSource
        {
            Strings = { Const = new[] { "a", "b" } }
        };

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(1);
        result[0].Chunks.Should().BeEquivalentTo(["a", "b"]);
        result[0].Delimiter.Should().BeNull();
    }

    [Fact]
    public async Task WhenStringsConstAndDelimiterConst_GetYieldsChunksWithDelimiter()
    {
        //arrange
        var sut = new JoinStringsTaskSource
        {
            Strings = { Const = new[] { "a", "b" } },
            Delimiter = { Const = ";" }
        };

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(1);
        result[0].Chunks.Should().BeEquivalentTo(["a", "b"]);
        result[0].Delimiter.Should().Be(";");
    }

    [Fact]
    public async Task WhenFirstAndSecondConst_GetYieldsTwoChunks()
    {
        //arrange
        var sut = new JoinStringsTaskSource
        {
            First = { Const = "a" },
            Second = { Const = "b" }
        };

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(1);
        result[0].Chunks.Should().BeEquivalentTo(["a", "b"]);
        result[0].Delimiter.Should().BeNull();
    }

    [Fact]
    public async Task WhenFirstToThirdConst_GetYieldsThreeChunks()
    {
        //arrange
        var sut = new JoinStringsTaskSource
        {
            First = { Const = "a" },
            Second = { Const = "b" },
            Third = { Const = "c" }
        };

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(1);
        result[0].Chunks.Should().BeEquivalentTo(["a", "b", "c"]);
    }

    [Fact]
    public async Task WhenAllFiveConst_GetYieldsFiveChunksWithDelimiter()
    {
        //arrange
        var sut = new JoinStringsTaskSource
        {
            First = { Const = "a" },
            Second = { Const = "b" },
            Third = { Const = "c" },
            Fourth = { Const = "d" },
            Fifth = { Const = "e" },
            Delimiter = { Const = "-" }
        };

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(1);
        result[0].Chunks.Should().BeEquivalentTo(["a", "b", "c", "d", "e"]);
        result[0].Delimiter.Should().Be("-");
    }

    [Fact]
    public async Task WhenStringsMulti_GetYieldsOneInputPerItem()
    {
        //arrange
        var sut = new JoinStringsTaskSource();
        sut.Strings.SetSource(Strings());

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(2);
        result[0].Chunks.Should().BeEquivalentTo(["a", "b"]);
        result[1].Chunks.Should().BeEquivalentTo(["c", "d"]);
        result.Should().AllSatisfy(i => i.Delimiter.Should().BeNull());
    }

    [Fact]
    public async Task WhenStringsMultiAndDelimiterConst_GetYieldsDelimiterPerItem()
    {
        //arrange
        var sut = new JoinStringsTaskSource
        {
            Delimiter = { Const = ";" }
        };
        sut.Strings.SetSource(Strings());

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(i => i.Delimiter.Should().Be(";"));
    }

    [Fact]
    public async Task WhenFirstAndSecondMulti_GetYieldsCombinationsUntilShortestIsExhausted()
    {
        //arrange
        var sut = new JoinStringsTaskSource();
        sut.First.SetSource(Firsts());
        sut.Second.SetSource(Seconds());

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(2);
        result[0].Chunks.Should().BeEquivalentTo(["f-1", "s-1"]);
        result[1].Chunks.Should().BeEquivalentTo(["f-2", "s-2"]);
    }

    [Fact]
    public async Task WhenFirstSecondAndThirdMulti_GetYieldsCombinationsUntilShortestIsExhausted()
    {
        //arrange
        var sut = new JoinStringsTaskSource();
        sut.First.SetSource(Firsts());
        sut.Second.SetSource(Seconds());
        sut.Third.SetSource(Thirds());

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(2);
        result[0].Chunks.Should().BeEquivalentTo(["f-1", "s-1", "t-1"]);
        result[1].Chunks.Should().BeEquivalentTo(["f-2", "s-2", "t-2"]);
    }

    [Fact]
    public void WhenAllConst_AllSingleIsTrue()
    {
        //arrange
        var sut = new JoinStringsTaskSource
        {
            First = { Const = "a" },
            Second = { Const = "b" },
            Delimiter = { Const = "-" }
        };

        //act
        var allSingle = sut.AllSingle;

        //assert
        allSingle.Should().BeTrue();
    }

    [Fact]
    public void WhenOnlyStringsSet_AllSingleIsTrue()
    {
        //arrange
        //AllSingle tracks Delimiter and First-Fifth, not Strings
        var sut = new JoinStringsTaskSource
        {
            Strings = { Const = new[] { "a" } }
        };

        //act
        var allSingle = sut.AllSingle;

        //assert
        allSingle.Should().BeTrue();
    }

    private static async IAsyncEnumerable<IEnumerable<string>> Strings()
    {
        yield return new[] { "a", "b" };
        await Task.Yield();
        yield return new[] { "c", "d" };
    }

    private static async IAsyncEnumerable<string> Firsts()
    {
        yield return "f-1";
        await Task.Yield();
        yield return "f-2";
        yield return "f-3";
    }

    private static async IAsyncEnumerable<string> Seconds()
    {
        yield return "s-1";
        await Task.Yield();
        yield return "s-2";
    }

    private static async IAsyncEnumerable<string> Thirds()
    {
        yield return "t-1";
        await Task.Yield();
        yield return "t-2";
    }
}
