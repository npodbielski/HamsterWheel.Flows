using FluentAssertions;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Utils;
using HamsterWheel.Flows.Tests.Utils;

namespace HamsterWheel.Flows.Tests.Blocks.Platform;

//HLinq (ExecuteHLinq) keeps shared state, so it races with other test classes calling it in parallel
[Collection("hlinq")]
public class MapBlockTests
{
    [Fact(Skip = "HLinq select[x.Name] not supported in 0.6.0-alpha")]
    public async Task WhenObjectPassedAsConst_ThenReturnsCorrectProperty()
    {
        //arrange
        var expected = "test";
        var item = new { Name = expected };
        var sut = new MapBlock();
        var pipeline = sut.InitBlock();
        sut.Inputs.Map.Const = nameof(item.Name);
        sut.Inputs.Object.Const = item;

        //act
        await pipeline.Run().WaitSeconds(1);
        var actual = await sut.Result.SingleValue;

        //assert
        actual.Should().Be(expected);
    }

    [Fact]
    public async Task WhenObjectPassedAsConstAndPropertyLowerCase_ThenReturnsCorrectProperty()
    {
        //arrange
        var expected = "test";
        var item = new { Name = expected };
        var sut = new MapBlock();
        var pipeline = sut.InitBlock();
        sut.Inputs.Map.Const = nameof(item.Name).ToLower();
        sut.Inputs.Object.Const = item;

        //act
        await pipeline.Run().WaitSeconds(1);
        var actual = await sut.Result.SingleValue;

        //assert
        actual.Should().Be(expected);
    }

    [Fact]
    public async Task WhenObjectPassedAsConstAndPropertyIsNested_ThenReturnsCorrectProperty()
    {
        //arrange
        var expected = "test";
        var item = new
        {
            Nested = new
            {
                Name = expected
            }
        };
        var sut = new MapBlock();
        var pipeline = sut.InitBlock();
        sut.Inputs.Map.Const = $"{nameof(item.Nested)}.{nameof(item.Nested.Name)}";
        sut.Inputs.Object.Const = item;

        //act
        await pipeline.Run().WaitSeconds(1);
        var actual = await sut.Result.SingleValue;

        //assert
        actual.Should().Be(expected);
    }

    [Fact]
    public async Task WhenObjectPassedAsConstAndPropertyIsNestedAndLowerCase_ThenReturnsCorrectProperty()
    {
        //arrange
        var expected = "test";
        var item = new
        {
            Nested = new
            {
                Name = expected
            }
        };
        var sut = new MapBlock();
        var pipeline = sut.InitBlock();
        sut.Inputs.Map.Const = $"{nameof(item.Nested).ToLower()}.{nameof(item.Nested.Name).ToLower()}";
        sut.Inputs.Object.Const = item;

        //act
        await pipeline.Run().WaitSeconds(1);
        var actual = await sut.Result.SingleValue;

        //assert
        actual.Should().Be(expected);
    }

    [Fact(Skip = "HLinq query syntax incompatible with current HLinq version")]
    public async Task WhenObjectAndHlinqMapWithNewPropertyAsString_ThenReturnsCorrectNewObject()
    {
        //arrange
        var item = new DummyTemplate
        {
            ResourceName = "core:source:Code/Test.cs",
            Name = "Test.cs",
            ExtensionName = "core",
            Type = "source",
            Contents = """
                       using System;

                       public class DummyTemplate
                       {
                           public string Name { get; set; }
                       }
                       """
        };
        var sut = new MapBlock();
        var pipeline = sut.InitBlock();
        sut.Inputs.Map.Const = "select[x.Name,Directory=\"core\",x.Contents]";
        sut.Inputs.Object.Const = item;
        sut.Inputs.Operation.Const = MapOperation.HLinq;

        //act
        await pipeline.Run().WaitSeconds(1);
        var actual = await sut.Result.SingleValue;

        //assert
        actual.Should().BeEquivalentTo(new
        {
            item.Name,
            item.Contents,
            Directory = "core"
        });
    }

    public class DummyTemplate
    {
        public string ResourceName { get; set; }
        public string Name { get; set; }
        public string ExtensionName { get; set; }
        public string Module { get; set; }
        public string Type { get; set; }
        public string Contents { get; set; }
    }
}
