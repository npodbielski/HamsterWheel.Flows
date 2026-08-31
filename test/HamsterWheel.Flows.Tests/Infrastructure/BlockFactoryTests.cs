using FluentAssertions;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Logic;
using HamsterWheel.Flows.IO;
using HamsterWheel.Flows.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace HamsterWheel.Flows.Tests.Infrastructure;

public class BlockFactoryTests
{
    [Fact]
    public void WhenCreateWithName_ThenBlockHasIdSet()
    {
        //arrange
        var services = new ServiceCollection().BuildServiceProvider();
        var factory = new BlockFactory(services, null);

        //act
        var block = factory.Create<IterateBlock>(name: "my-block");

        //assert
        block.Id.Should().Be("my-block");
    }

    [Fact]
    public void WhenCreateWithoutName_ThenBlockHasNoId()
    {
        //arrange
        var services = new ServiceCollection().BuildServiceProvider();
        var factory = new BlockFactory(services, null);

        //act
        var block = factory.Create<IterateBlock>();

        //assert
        block.Id.Should().BeNull();
    }

    [Fact]
    public void WhenCreate_ThenBlockIsOfTypeT()
    {
        //arrange
        var services = new ServiceCollection().BuildServiceProvider();
        var factory = new BlockFactory(services, null);

        //act
        var block = factory.Create<IterateBlock>();

        //assert
        block.Should().BeOfType<IterateBlock>();
    }

    [Fact]
    public void WhenCreateWithGlobalInputTransformer_ThenTransformerIsSet()
    {
        //arrange
        var services = new ServiceCollection().BuildServiceProvider();
        var transformer = Substitute.For<IGlobalInputTransformer>();
        var transformFunc = Substitute.For<Func<IPipelineLogger, object?, object?>>();
        transformer.GetTransformation<IterateBlock>().Returns(transformFunc);
        var factory = new BlockFactory(services, transformer);

        //act
        var block = factory.Create<IterateBlock>();

        //assert
        block.InputTransformer.Should().BeSameAs(transformFunc);
    }
}
