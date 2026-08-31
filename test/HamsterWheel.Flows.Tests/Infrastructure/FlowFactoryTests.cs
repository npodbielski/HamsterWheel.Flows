using FluentAssertions;
using HamsterWheel.Flows;
using HamsterWheel.Flows.Runner;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace HamsterWheel.Flows.Tests.Infrastructure;

public class FlowFactoryTests
{
    [Fact]
    public void WhenCreateWithValidFlowName_ThenReturnsFlowInstance()
    {
        //arrange
        var services = CreateServiceProviderWithFlow<TestFlow>("test-flow");
        var factory = new FlowFactory(services);

        //act
        var flow = factory.Create(c => c.FlowName = new FlowName("test-flow"));

        //assert
        flow.Should().BeOfType<TestFlow>();
    }

    [Fact]
    public void WhenCreateWithNonExistentFlow_ThenThrowsFlowNotFoundException()
    {
        //arrange
        var services = CreateServiceProviderWithFlow<TestFlow>("test-flow");
        var factory = new FlowFactory(services);

        //act
        var action = () => factory.Create(c => c.FlowName = new FlowName("nonexistent"));

        //assert
        action.Should().Throw<Exception>().WithMessage("*not found*");
    }

    [Fact]
    public void WhenExistsWithValidFlowName_ThenReturnsTrue()
    {
        //arrange
        var services = CreateServiceProviderWithFlow<TestFlow>("test-flow");
        var factory = new FlowFactory(services);

        //act
        var result = factory.Exists(new FlowName("test-flow"));

        //assert
        result.Should().BeTrue();
    }

    [Fact]
    public void WhenExistsWithNonExistentFlowName_ThenReturnsFalse()
    {
        //arrange
        var services = CreateServiceProviderWithFlow<TestFlow>("test-flow");
        var factory = new FlowFactory(services);

        //act
        var result = factory.Exists(new FlowName("nonexistent"));

        //assert
        result.Should().BeFalse();
    }

    private static IServiceProvider CreateServiceProviderWithFlow<TFlow>(string flowName)
        where TFlow : class, IFlow
    {
        var services = new ServiceCollection();
        services.AddKeyedTransient<IFlow, TFlow>(flowName);
        return services.BuildServiceProvider();
    }

    private class TestFlow : IFlow
    {
        public TimeSpan? AverageTime => null;
        public string Name => "test-flow";
        public bool DoesAllowAnonymousRuns => true;
        public string Definition => "";
        public string? InputSchema => null;
        public string? OutputSchema => null;
        public bool HaveInput => false;
        public bool HaveOutput => false;
        public string Version => "1.0";
        public Type? InputType => null;
        public Type? OutputType => null;
        public object? BuildOutput() => null;
        public Task ApplyToPipeline(Pipelines.IPipeline pipeline, Pipelines.IFlowGlobalInputBag resolver) => Task.CompletedTask;
    }
}
