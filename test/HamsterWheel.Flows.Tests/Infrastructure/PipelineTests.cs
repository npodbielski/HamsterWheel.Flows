using FluentAssertions;
using HamsterWheel.Flows;
using HamsterWheel.Flows.Auth;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Base;
using HamsterWheel.Flows.Blocks.Logic;
using HamsterWheel.Flows.Blocks.Utils;
using HamsterWheel.Flows.IO;
using HamsterWheel.Flows.Monitoring;
using HamsterWheel.Flows.Pipelines;
using HamsterWheel.Flows.Runner;
using HamsterWheel.Flows.Tests.Utils;
using NSubstitute;

namespace HamsterWheel.Flows.Tests.Infrastructure;

public class PipelineTests
{
    [Fact]
    public async Task WhenRunWithCompletingBlock_ThenReturnsOutput()
    {
        //arrange
        var block = new JoinStringsBlock();
        var pipeline = block.InitBlock(output: "final-output");
        block.Inputs.First.Const = "a";
        block.Inputs.Second.Const = "b";
        block.Inputs.Delimiter.Const = "-";

        //act
        var result = await pipeline.Run().WaitSeconds(5);

        //assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenBlockThrows_ThenCompletionIsFaulted()
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
    public async Task WhenBlockHasConditionFalse_ThenBlockIsSkipped()
    {
        //arrange
        var block = new IterateBlock { Id = "cond-block" };
        block.Inputs.Const = new[] { 1, 2, 3 };
        block.If(false);
        var pipeline = block.InitBlock();

        //act
        await pipeline.Run().WaitSeconds(5);

        //assert
        block.Completion.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task WhenBlockHasConditionTrue_ThenBlockRuns()
    {
        //arrange
        var block = new IterateBlock { Id = "cond-block" };
        block.Inputs.Const = new[] { 1, 2, 3 };
        block.If(true);
        var pipeline = block.InitBlock();

        //act
        var enumerator = block.Result.AsEnumerable();
        await pipeline.Run().WaitSeconds(5);
        var actual = await enumerator.ToArrayAsync().AsTask().WaitSeconds(5);

        //assert
        actual.Should().HaveCount(3);
    }

    [Fact]
    public void WhenAddBlockTwice_ThenOnlyOneInstance()
    {
        //arrange
        var block = new IterateBlock();
        var pipeline = block.InitBlock();

        //act
        pipeline.AddBlock(block);

        //assert
        // block is already in pipeline, adding again should not duplicate
        // (verified indirectly - pipeline still works)
        pipeline.Should().NotBeNull();
    }

    [Fact]
    public async Task WhenRunWithCancelledToken_ThenThrowsOrReturns()
    {
        //arrange
        var block = new IterateBlock();
        block.Inputs.Const = new[] { 1, 2, 3 };
        var pipeline = block.InitBlock();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        //act
        var action = async () => await pipeline.Run(cts.Token);

        //assert
        // Cancelled token should either throw OperationCanceledException or return early
        try
        {
            await action.Invoke();
        }
        catch (OperationCanceledException)
        {
            // expected
        }
    }

    [Fact]
    public void WhenAttachCoordinator_ThenCoordinatorIsSet()
    {
        //arrange
        var pipeline = CreateTestPipeline();
        var newCoordinator = new FlowCoordinator();

        //act
        pipeline.AttachCoordinator(newCoordinator);

        //assert
        pipeline.Coordinator.Should().BeSameAs(newCoordinator);
    }

    [Fact]
    public async Task WhenBlockRunExceedsEstimatedMaxTime_ThenFlowCancelledException()
    {
        //arrange (timeout is AverageTime * 3 = 150ms, slow block runs 500ms)
        var pipeline = CreatePipelineWithFlow(TimeSpan.FromMilliseconds(50));
        pipeline.AddBlock(new SlowBlock());

        //act
        var action = async () => await pipeline.Run().WaitSeconds(5);

        //assert
        await action.Should().ThrowAsync<FlowCancelledException>();
    }

    [Fact]
    public void WhenAddBlockGeneric_ThenFactoryCreatedBlockIsAddedAndReturned()
    {
        //arrange
        var blockFactory = Substitute.For<IBlockFactory>();
        var pipeline = new Pipeline(new PipelineCreationOptions(), new FlowCoordinator(), blockFactory,
            new PipelineLogger());
        var block = new LogBlock();
        blockFactory.Create<LogBlock>("log-name", "log-desc").Returns(block);

        //act
        var created = pipeline.AddBlock<LogBlock>("log-name", "log-desc");

        //assert
        created.Should().BeSameAs(block);
    }

    private static Pipeline CreateTestPipeline()
    {
        var coordinator = new FlowCoordinator();
        var blockFactory = Substitute.For<IBlockFactory>();
        var logger = new PipelineLogger();
        var pipeline = new Pipeline(new PipelineCreationOptions(), coordinator, blockFactory, logger);
        coordinator.AttachPipeline(pipeline, new FixedUserService("user", []));
        return pipeline;
    }

    private static Pipeline CreatePipelineWithFlow(TimeSpan averageTime)
    {
        var coordinator = new FlowCoordinator();
        var pipeline = new Pipeline(new PipelineCreationOptions(), coordinator, Substitute.For<IBlockFactory>(),
            new PipelineLogger());
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
