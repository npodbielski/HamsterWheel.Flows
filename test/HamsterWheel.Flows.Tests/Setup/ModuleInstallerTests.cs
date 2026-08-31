using FluentAssertions;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Data.Serialization;
using HamsterWheel.Flows.Pipelines;
using HamsterWheel.Flows.Runner;
using HamsterWheel.Flows.Setup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HamsterWheel.Flows.Tests.Setup;

public class ModuleInstallerTests
{
    [Fact]
    public void WhenAddFlowsModuleCalled_ThenFlowsServicesResolve()
    {
        //arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFlowsModule();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        //act
        var scheduler = provider.GetRequiredService<IFlowScheduler>();
        var pipelineFactory = scope.ServiceProvider.GetRequiredService<IPipelineFactory>();
        var blockFactory = scope.ServiceProvider.GetRequiredService<IBlockFactory>();

        //assert
        scheduler.Should().NotBeNull();
        pipelineFactory.Should().NotBeNull();
        blockFactory.Should().NotBeNull();
    }

    [Fact]
    public void WhenAddFlowsWithDependenciesCalled_ThenRunnerChainResolves()
    {
        //arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFlowsWithDependencies();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        //act
        var runner = scope.ServiceProvider.GetRequiredService<IFlowRunner>();

        //assert
        runner.Should().NotBeNull();
    }

    [Fact]
    public void WhenDefaultFallbackConverterUsed_ThenItNeverConverts()
    {
        //arrange
        var converter = new DefaultFallbackConverter();

        //act + assert
        converter.CanConvert("value", typeof(int)).Should().BeFalse();
        converter.ConvertTo("value", typeof(int)).Should().BeNull();
        converter.Priority.Should().Be(int.MaxValue);
    }

    [Fact]
    public void WhenDefaultExtraInputBagUsed_ThenBagIsEmpty()
    {
        //arrange
        var bag = new DefaultExtraInputBag();

        //assert
        bag.Bag.Should().BeEmpty();
    }
}
