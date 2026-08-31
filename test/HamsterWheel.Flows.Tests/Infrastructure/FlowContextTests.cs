using FluentAssertions;
using HamsterWheel.Flows;
using HamsterWheel.Flows.Pipelines;
using NSubstitute;

namespace HamsterWheel.Flows.Tests.Infrastructure;

public class FlowContextTests
{
    [Fact]
    public void WhenFlowContextCreated_ThenPipelineIsSet()
    {
        //arrange
        var pipeline = Substitute.For<IPipeline>();
        var coordinator = Substitute.For<IFlowCoordinator>();

        //act
        var context = new FlowContext(coordinator) { Pipeline = pipeline };

        //assert
        context.Pipeline.Should().BeSameAs(pipeline);
    }

    [Fact]
    public void WhenSetFlowOnContext_ThenFlowIsStored()
    {
        //arrange
        var pipeline = Substitute.For<IPipeline>();
        var coordinator = Substitute.For<IFlowCoordinator>();
        var context = new FlowContext(coordinator) { Pipeline = pipeline };
        var flow = Substitute.For<IFlow>();
        flow.BuildOutput().Returns("output");

        //act
        context.SetFlow(flow, "input-data");

        //assert
        context.Output.Should().Be("output");
        context.Input.Should().Be("input-data");
    }

    [Fact]
    public void WhenGetChildContext_ThenChildHasSamePipeline()
    {
        //arrange
        var pipeline = Substitute.For<IPipeline>();
        var coordinator = Substitute.For<IFlowCoordinator>();
        var context = new FlowContext(coordinator) { Pipeline = pipeline };
        var flow = Substitute.For<IFlow>();
        flow.BuildOutput().Returns(null);
        context.SetFlow(flow, null);

        //act
        var child = context.GetChildContext();

        //assert
        child.Should().NotBeNull();
        child.Pipeline.Should().BeSameAs(pipeline);
    }

    [Fact]
    public void WhenSetSuccessWithISuccessOutput_ThenSuccessIsSet()
    {
        //arrange
        var pipeline = Substitute.For<IPipeline>();
        var coordinator = Substitute.For<IFlowCoordinator>();
        var context = new FlowContext(coordinator) { Pipeline = pipeline };
        var flow = Substitute.For<IFlow>();
        var output = new TestSuccess();
        flow.BuildOutput().Returns(output);
        context.SetFlow(flow, null);

        //act
        context.SetSuccess(false);

        //assert
        output.Success.Should().BeTrue();
    }

    [Fact]
    public void WhenSetSuccessWithNonSuccessOutput_ThenDoesNotThrow()
    {
        //arrange
        var pipeline = Substitute.For<IPipeline>();
        var coordinator = Substitute.For<IFlowCoordinator>();
        var context = new FlowContext(coordinator) { Pipeline = pipeline };
        var flow = Substitute.For<IFlow>();
        flow.BuildOutput().Returns("plain-string");
        context.SetFlow(flow, null);

        //act
        var action = () => context.SetSuccess(false);

        //assert
        action.Should().NotThrow();
    }

    private class TestSuccess : ISuccess
    {
        public bool Success { get; set; }
    }
}
