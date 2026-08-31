using FluentAssertions;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Utils;

namespace HamsterWheel.Flows.Tests.Blocks.Utils;

public class MapInputTaskSourceTests
{
    [Fact]
    public async Task WhenAllConst_GetYieldsSingleInput()
    {
        //arrange
        var sut = new MapInputTaskSource
        {
            Operation = { Const = MapOperation.HLinq },
            Map = { Const = "x.name" },
            Object = { Const = "payload" }
        };

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(1);
        result[0].Operation.Should().Be(MapOperation.HLinq);
        result[0].Map.Should().Be("x.name");
        result[0].Object.Should().Be("payload");
    }

    [Fact]
    public void WhenAllConst_AllSingleIsTrue()
    {
        //arrange
        var sut = new MapInputTaskSource
        {
            Map = { Const = "x.name" },
            Object = { Const = "payload" }
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
        var sut = new MapInputTaskSource();
        sut.Operation.SetSource(Task.FromResult(MapOperation.Property));
        sut.Map.SetSource(Task.FromResult("name"));
        sut.Object.SetSource(Task.FromResult<object>("payload"));

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(1);
        result[0].Operation.Should().Be(MapOperation.Property);
        result[0].Map.Should().Be("name");
        result[0].Object.Should().Be("payload");
    }

    [Fact]
    public async Task WhenMultiMapAndObject_GetYieldsCombinationsUntilShortestIsExhausted()
    {
        //arrange
        //Operation keeps its default const value: SetSource(IAsyncEnumerable) does not clear the const flag
        var sut = new MapInputTaskSource();
        sut.Map.SetSource(Maps());
        sut.Object.SetSource(Objects());

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(2);
        result[0].Operation.Should().Be(MapOperation.Property);
        result[0].Map.Should().Be("map-1");
        result[0].Object.Should().Be("object-1");
        result[1].Operation.Should().Be(MapOperation.Property);
        result[1].Map.Should().Be("map-2");
        result[1].Object.Should().Be("object-2");
    }

    [Fact]
    public async Task WhenMapConstAndObjectMulti_GetYieldsOneInputPerObject()
    {
        //arrange
        var sut = new MapInputTaskSource
        {
            Map = { Const = "name" }
        };
        sut.Object.SetSource(Objects());

        //act
        var result = await sut.Get().ToListAsync();

        //assert
        result.Should().HaveCount(2);
        result[0].Object.Should().Be("object-1");
        result[1].Object.Should().Be("object-2");
        result.Should().AllSatisfy(i =>
        {
            i.Operation.Should().Be(MapOperation.Property);
            i.Map.Should().Be("name");
        });
    }

    private static async IAsyncEnumerable<string> Maps()
    {
        yield return "map-1";
        await Task.Yield();
        yield return "map-2";
        yield return "map-3";
    }

    private static async IAsyncEnumerable<object> Objects()
    {
        yield return "object-1";
        await Task.Yield();
        yield return "object-2";
    }
}
