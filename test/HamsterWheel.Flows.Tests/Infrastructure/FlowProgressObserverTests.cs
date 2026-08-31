using FluentAssertions;
using HamsterWheel.Flows;
using HamsterWheel.Flows.Auth;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Base;
using HamsterWheel.Flows.Blocks.Utils;
using HamsterWheel.Flows.IO;
using HamsterWheel.Flows.Monitoring;
using HamsterWheel.Flows.Pipelines;
using HamsterWheel.Flows.Runner;
using HamsterWheel.Flows.Tests.Utils;
using NSubstitute;

namespace HamsterWheel.Flows.Tests.Infrastructure;

public class FlowProgressObserverTests
{
    [Fact]
    public async Task WhenFlowRuns_ThenObserverReceivesNotificationsInOrder()
    {
        //arrange
        var observer = new RecordingObserver();
        var block = new JoinStringsBlock();
        var pipeline = block.InitBlock(observer: observer);
        block.Inputs.First.Const = "a";
        block.Inputs.Second.Const = "b";
        block.Inputs.Delimiter.Const = "-";

        //act
        await pipeline.Run().WaitSeconds(5);

        //assert
        observer.Events.Should().ContainInOrder(nameof(IFlowProgressObserver.FlowStarted),
            $"BlockCompleted:{block.Id}", nameof(IFlowProgressObserver.FlowCompleted));
        observer.StartedBlockCount.Should().Be(1);
        observer.StartedRun.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenBlockFails_ThenBlockFailedCarriesTheBlockException()
    {
        //arrange
        var observer = new RecordingObserver();
        var block = new ThrowingBlock { Id = "throwing-block" };
        var pipeline = block.InitBlock(observer: observer);

        //act
        var action = async () => await pipeline.Run().WaitSeconds(5);

        //assert
        await action.Should().ThrowAsync<Exception>();
        var (failedBlock, exception) = observer.FailedBlocks.Should().ContainSingle().Subject;
        failedBlock.Should().BeSameAs(block);
        exception.Should().BeOfType<BlockOperationException>().Which.Message.Should().Contain(block.Id);
        var innerException = exception.Should().BeOfType<BlockOperationException>().Which.InnerException;
        innerException.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Be("Test exception");
    }

    [Fact]
    public async Task WhenFlowCompletes_ThenFlowCompletedCarriesTheFlowOutput()
    {
        //arrange
        var output = new FlowOutput("result");
        var observer = new RecordingObserver();
        var block = new JoinStringsBlock();
        var pipeline = block.InitBlock(output: output, observer: observer);
        block.Inputs.First.Const = "a";
        block.Inputs.Second.Const = "b";
        block.Inputs.Delimiter.Const = "-";

        //act
        var result = await pipeline.Run().WaitSeconds(5);

        //assert
        observer.CompletedOutput.Should().BeSameAs(output);
        result.Should().BeSameAs(output);
    }

    [Fact]
    public async Task WhenObserverThrows_ThenFlowStillCompletes()
    {
        //arrange
        var observer = new ThrowingObserver();
        var block = new JoinStringsBlock();
        var pipeline = block.InitBlock(output: "flow-output", observer: observer);
        block.Inputs.First.Const = "a";
        block.Inputs.Second.Const = "b";
        block.Inputs.Delimiter.Const = "-";

        //act
        var result = await pipeline.Run().WaitSeconds(5);

        //assert
        result.Should().Be("flow-output");
        observer.Calls.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task WhenNoObserverRegistered_ThenFlowStillCompletes()
    {
        //arrange
        var block = new JoinStringsBlock();
        var pipeline = block.InitBlock(output: "flow-output");
        block.Inputs.First.Const = "a";
        block.Inputs.Second.Const = "b";
        block.Inputs.Delimiter.Const = "-";

        //act
        var result = await pipeline.Run().WaitSeconds(5);

        //assert
        result.Should().Be("flow-output");
    }

    [Fact]
    public async Task WhenNoObserverRegisteredAndBlockFails_ThenFlowStillThrows()
    {
        //arrange
        var block = new ThrowingBlock();
        var pipeline = block.InitBlock();

        //act
        var action = async () => await pipeline.Run().WaitSeconds(5);

        //assert
        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task WhenRunExceedsMaxTime_ThenFlowFailedIsNotifiedWithFlowCancelledException()
    {
        //arrange (timeout is AverageTime * 3 = 150ms, slow block runs 500ms)
        var observer = new RecordingObserver();
        var pipeline = CreatePipelineWithFlow(TimeSpan.FromMilliseconds(50), observer);
        pipeline.AddBlock(new SlowBlock());

        //act
        var action = async () => await pipeline.Run().WaitSeconds(5);

        //assert
        await action.Should().ThrowAsync<FlowCancelledException>();
        observer.FailedError.Should().BeOfType<FlowCancelledException>();
    }

    private static Pipeline CreatePipelineWithFlow(TimeSpan averageTime, IFlowProgressObserver? observer)
    {
        var coordinator = new FlowCoordinator();
        var pipeline = new Pipeline(new PipelineCreationOptions(), coordinator, Substitute.For<IBlockFactory>(),
            new PipelineLogger(), observer);
        coordinator.AttachPipeline(pipeline, new FixedUserService("user", []));
        var flow = Substitute.For<IFlow>();
        flow.Name.Returns("test-flow");
        flow.Version.Returns("1.0");
        flow.AverageTime.Returns(averageTime);
        flow.DoesAllowAnonymousRuns.Returns(true);
        coordinator.Authorize(flow);
        var context = new FlowContext(coordinator) { Pipeline = pipeline };
        context.SetFlow(flow, null);
        pipeline.AttachFlowContext(context);
        return pipeline;
    }

    private sealed record FlowOutput(string Value);

    private sealed class RecordingObserver : IFlowProgressObserver
    {
        public List<string> Events { get; } = [];
        public FlowRunIdentity? StartedRun { get; private set; }
        public int StartedBlockCount { get; private set; }
        public List<(IPipelineBlock Block, Exception Exception)> FailedBlocks { get; } = [];
        public object? CompletedOutput { get; private set; }
        public Exception? FailedError { get; private set; }

        public void FlowStarted(FlowRunIdentity run, int blockCount)
        {
            StartedRun = run;
            StartedBlockCount = blockCount;
            Events.Add(nameof(IFlowProgressObserver.FlowStarted));
        }

        public void BlockCompleted(FlowRunIdentity run, IPipelineBlock block)
        {
            Events.Add($"BlockCompleted:{block.Id}");
        }

        public void BlockFailed(FlowRunIdentity run, IPipelineBlock block, Exception exception)
        {
            FailedBlocks.Add((block, exception));
            Events.Add($"BlockFailed:{block.Id}");
        }

        public void FlowCompleted(FlowRunIdentity run, object? output)
        {
            CompletedOutput = output;
            Events.Add(nameof(IFlowProgressObserver.FlowCompleted));
        }

        public void FlowFailed(FlowRunIdentity run, Exception error)
        {
            FailedError = error;
            Events.Add(nameof(IFlowProgressObserver.FlowFailed));
        }
    }

    private sealed class ThrowingObserver : IFlowProgressObserver
    {
        public int Calls { get; private set; }

        public void FlowStarted(FlowRunIdentity run, int blockCount) => Fail();
        public void BlockCompleted(FlowRunIdentity run, IPipelineBlock block) => Fail();
        public void BlockFailed(FlowRunIdentity run, IPipelineBlock block, Exception exception) => Fail();
        public void FlowCompleted(FlowRunIdentity run, object? output) => Fail();
        public void FlowFailed(FlowRunIdentity run, Exception error) => Fail();

        private void Fail()
        {
            Calls++;
            throw new InvalidOperationException("observer failure");
        }
    }

    private class SlowBlock : NoInNoOutPipelineBlock
    {
        public override IInputInfo Inputs => null!;
        public override Task RunForInput() => Task.Delay(500);
    }

    private class ThrowingBlock : NoInNoOutPipelineBlock
    {
        public override IInputInfo Inputs => null!;
        public override Task RunForInput() =>
            throw new InvalidOperationException("Test exception");
    }
}
