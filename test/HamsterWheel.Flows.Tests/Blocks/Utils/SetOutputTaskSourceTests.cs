using FluentAssertions;
using HamsterWheel.Data.Mapper;
using HamsterWheel.Flows.Blocks.Utils;
using static HamsterWheel.Data.Mapper.DataMapper;

namespace HamsterWheel.Flows.Tests.Blocks.Utils;

public class SetOutputTaskSourceTests
{
    [Fact]
    public async Task WhenValueConstOnly_GetYieldsInputWithoutPathAndMap()
    {
        //arrange
        var sut = new SetOutputTaskSource
        {
            Value = { Const = 42 }
        };

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(1);
        result[0].PropertyPath.Should().BeNull();
        result[0].Value.Should().Be(42);
        result[0].Map.Should().BeNull();
    }

    [Fact]
    public async Task WhenAllConst_GetYieldsSingleInput()
    {
        //arrange
        var sut = new SetOutputTaskSource
        {
            PropertyPath = { Const = "a.b" },
            Value = { Const = "v" },
            Map = { Const = [(AnyProperty, AnyProperty)] }
        };

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(1);
        result[0].PropertyPath.Should().Be("a.b");
        result[0].Value.Should().Be("v");
        result[0].Map.Should().NotBeNull();
    }

    [Fact]
    public void WhenAllConst_AllSingleIsTrue()
    {
        //arrange
        var sut = new SetOutputTaskSource
        {
            PropertyPath = { Const = "a.b" },
            Value = { Const = "v" }
        };

        //act
        var allSingle = sut.AllSingle;

        //assert
        allSingle.Should().BeTrue();
    }

    [Fact]
    public async Task WhenSingleTaskSources_GetYieldsSingleInput()
    {
        //arrange
        var sut = new SetOutputTaskSource();
        sut.PropertyPath.SetSource(Task.FromResult("a.b"));
        sut.Value.SetSource(Task.FromResult<object?>("v"));

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(1);
        result[0].PropertyPath.Should().Be("a.b");
        result[0].Value.Should().Be("v");
        result[0].Map.Should().BeNull();
    }

    [Fact]
    public async Task WhenPropertyPathAndValueMulti_GetYieldsCombinationsUntilShortestIsExhausted()
    {
        //arrange
        var sut = new SetOutputTaskSource();
        sut.PropertyPath.SetSource(PropertyPaths());
        sut.Value.SetSource(Values());

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(2);
        result[0].PropertyPath.Should().Be("path-1");
        result[0].Value.Should().Be("value-1");
        result[1].PropertyPath.Should().Be("path-2");
        result[1].Value.Should().Be("value-2");
        result.Should().AllSatisfy(i => i.Map.Should().BeNull());
    }

    [Fact]
    public async Task WhenAllThreeMulti_GetYieldsCombinationsUntilShortestIsExhausted()
    {
        //arrange
        var sut = new SetOutputTaskSource();
        sut.PropertyPath.SetSource(PropertyPaths());
        sut.Value.SetSource(Values());
        sut.Map.SetSource(Maps());

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(2);
        result[0].PropertyPath.Should().Be("path-1");
        result[0].Value.Should().Be("value-1");
        result[0].Map.Should().NotBeNull();
        result[1].PropertyPath.Should().Be("path-2");
        result[1].Value.Should().Be("value-2");
        result[1].Map.Should().NotBeNull();
    }

    private static async IAsyncEnumerable<string?> PropertyPaths()
    {
        yield return "path-1";
        await Task.Yield();
        yield return "path-2";
    }

    private static async IAsyncEnumerable<object?> Values()
    {
        yield return "value-1";
        await Task.Yield();
        yield return "value-2";
        yield return "value-3";
    }

    private static async IAsyncEnumerable<Map?> Maps()
    {
        yield return [(AnyProperty, AnyProperty)];
        await Task.Yield();
        yield return [(AnyProperty, AnyProperty), (AnyProperty, AnyProperty)];
    }
}
