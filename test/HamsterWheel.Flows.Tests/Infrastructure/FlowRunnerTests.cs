using FluentAssertions;
using HamsterWheel.Flows;
using HamsterWheel.Flows.Pipelines;
using HamsterWheel.Flows.Runner;
using NSubstitute;

namespace HamsterWheel.Flows.Tests.Infrastructure;

public class FlowRunnerTests
{
    [Fact]
    public async Task WhenRunAsync_ThenPipelineIsCreatedAndExecuted()
    {
        //arrange
        var pipelineFactory = Substitute.For<IPipelineFactory>();
        var flowFactory = Substitute.For<IFlowFactory>();
        var flowApplier = Substitute.For<IFlowApplier>();
        var pipeline = Substitute.For<IPipeline>();
        var flow = Substitute.For<IFlow>();
        flow.HaveOutput.Returns(true);
        pipeline.Run(Arg.Any<CancellationToken>()).Returns((object?)"pipeline-result");

        var runner = new FlowRunner(pipelineFactory, flowFactory, flowApplier);
        var message = Substitute.For<IScheduledFlowData>();
        message.ScheduledId.Returns(Guid.NewGuid());
        message.UserId.Returns("user-1");
        message.FlowName.Returns(new FlowName("test-flow"));
        message.IsSubFlow.Returns(false);
        message.Input.Returns(null);

        pipelineFactory.Create(Arg.Any<Action<IPipelineCreationOptions>>()).Returns(pipeline);
        flowFactory.Create(Arg.Any<Action<IFlowCreationOptions>>()).Returns(flow);

        //act
        var result = await runner.RunAsync(message, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result!.Output.Should().Be("pipeline-result");
    }

    [Fact]
    public async Task WhenRunAsyncWithNoOutputFlow_ThenResultHasNoOutput()
    {
        //arrange
        var pipelineFactory = Substitute.For<IPipelineFactory>();
        var flowFactory = Substitute.For<IFlowFactory>();
        var flowApplier = Substitute.For<IFlowApplier>();
        var pipeline = Substitute.For<IPipeline>();
        var flow = Substitute.For<IFlow>();
        flow.HaveOutput.Returns(false);
        pipeline.Run(Arg.Any<CancellationToken>()).Returns((object?)null);

        var runner = new FlowRunner(pipelineFactory, flowFactory, flowApplier);
        var message = Substitute.For<IScheduledFlowData>();
        message.ScheduledId.Returns(Guid.NewGuid());
        message.UserId.Returns("user-1");
        message.FlowName.Returns(new FlowName("test-flow"));
        message.IsSubFlow.Returns(false);
        message.Input.Returns(null);

        pipelineFactory.Create(Arg.Any<Action<IPipelineCreationOptions>>()).Returns(pipeline);
        flowFactory.Create(Arg.Any<Action<IFlowCreationOptions>>()).Returns(flow);

        //act
        var result = await runner.RunAsync(message, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result!.Output.Should().BeNull();
    }

    [Fact]
    public async Task WhenRunAsync_ThenFlowApplierIsCalled()
    {
        //arrange
        var pipelineFactory = Substitute.For<IPipelineFactory>();
        var flowFactory = Substitute.For<IFlowFactory>();
        var flowApplier = Substitute.For<IFlowApplier>();
        var pipeline = Substitute.For<IPipeline>();
        var flow = Substitute.For<IFlow>();
        flow.HaveOutput.Returns(true);
        pipeline.Run(Arg.Any<CancellationToken>()).Returns((object?)"result");
        var input = new { Value = 42 };

        var runner = new FlowRunner(pipelineFactory, flowFactory, flowApplier);
        var message = Substitute.For<IScheduledFlowData>();
        message.ScheduledId.Returns(Guid.NewGuid());
        message.UserId.Returns("user-1");
        message.FlowName.Returns(new FlowName("test-flow"));
        message.IsSubFlow.Returns(false);
        message.Input.Returns(input);

        pipelineFactory.Create(Arg.Any<Action<IPipelineCreationOptions>>()).Returns(pipeline);
        flowFactory.Create(Arg.Any<Action<IFlowCreationOptions>>()).Returns(flow);

        //act
        await runner.RunAsync(message, CancellationToken.None);

        //assert
        await flowApplier.Received(1).Apply(pipeline, flow, input);
    }
}
