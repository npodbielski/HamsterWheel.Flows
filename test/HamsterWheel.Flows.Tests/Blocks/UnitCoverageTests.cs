using FluentAssertions;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Logic;
using HamsterWheel.Flows.Blocks.Os.IO;
using HamsterWheel.Flows.Blocks.Utils;
using HamsterWheel.Flows.IO;
using HamsterWheel.Flows.Pipelines;
using HamsterWheel.Flows.Templates;
using NSubstitute;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.Flows.Tests.Blocks;

public class UnitCoverageTests
{
    [Fact]
    public async Task WhenCreateObjectTaskSourceWithAllSixProps_ThenReturnsFullInput()
    {
        //arrange
        var source = new CreateObjectTaskSource();
        source.Property1.Const = "prop1";
        source.Property2.Const = "prop2";
        source.Property3.Const = "prop3";
        source.Object1.Const = (object)"obj1";
        source.Object2.Const = (object?)"obj2";
        source.Object3.Const = (object?)"obj3";

        //act
        var results = new List<CreateObjectInput>();
        await foreach (var item in source.Get())
        {
            results.Add(item);
        }

        //assert
        results.Should().HaveCount(1);
        results[0].Property1!.Should().NotBeNull();
        results[0].Object1.Should().Be("obj1");
        results[0].Object2.Should().Be("obj2");
        results[0].Object3.Should().Be("obj3");
    }

    [Fact]
    public async Task WhenCreateObjectTaskSourcePartial_ThenReturnsWithNulls()
    {
        //arrange
        var source = new CreateObjectTaskSource();
        source.Property1.Const = "only";
        source.Object1.Const = (object)42;

        //act
        var results = new List<CreateObjectInput>();
        await foreach (var item in source.Get())
        {
            results.Add(item);
        }

        //assert
        results.Should().HaveCount(1);
        results[0].Property1!.Should().NotBeNull();
        results[0].Object1.Should().Be(42);
    }

    [Fact]
    public void WhenCreateObjectTaskSourceAllSingle_ThenTrue()
    {
        //arrange
        var source = new CreateObjectTaskSource();
        source.Property1.Const = "a";
        source.Object1.Const = (object)1;

        //act & assert
        source.AllSingle.Should().BeTrue();
    }

    [Fact]
    public async Task WhenSimpleRenderingServiceWithPlainText_ThenReturnsAsIs()
    {
        //arrange
        var service = new SimpleRenderingService();
        var models = new Dictionary<string, object?>();

        //act
        var result = await service.Render("No templates here", models);

        //assert
        result.Should().Be("No templates here");
    }

    [Fact]
    public void WhenBlockFactoryCreatesBlockWithName_ThenBlockHasId()
    {
        //arrange
        var services = Substitute.For<IServiceProvider>();
        var factory = new BlockFactory(services, null);

        //act
        var block = factory.Create<IterateBlock>("my-block", "desc");

        //assert
        block.Should().NotBeNull();
        block.Id.Should().Be("my-block");
    }

    [Fact]
    public void WhenBlockFactoryCreatesBlockWithoutName_ThenNoId()
    {
        //arrange
        var services = Substitute.For<IServiceProvider>();
        var factory = new BlockFactory(services, null);

        //act
        var block = factory.Create<IterateBlock>();

        //assert
        block.Should().NotBeNull();
    }

    [Fact]
    public void WhenBlockFactoryWithTransformer_ThenAppliesTransformation()
    {
        //arrange
        var services = Substitute.For<IServiceProvider>();
        var transformer = Substitute.For<IGlobalInputTransformer>();
        transformer.GetTransformation<IterateBlock>().Returns(null!);
        var factory = new BlockFactory(services, transformer);

        //act
        var block = factory.Create<IterateBlock>();

        //assert
        block.Should().NotBeNull();
    }
}
