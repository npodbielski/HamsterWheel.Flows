using FluentAssertions;
using HamsterWheel.Flows;
using HamsterWheel.Flows.Auth;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Base;
using HamsterWheel.Flows.Blocks.Logic;
using HamsterWheel.Flows.IO;
using HamsterWheel.Flows.Monitoring;
using HamsterWheel.Flows.Pipelines;
using HamsterWheel.Flows.Runner;
using HamsterWheel.Flows.Tests.Utils;
using NSubstitute;

namespace HamsterWheel.Flows.Tests.Infrastructure;

public class PipelineAdvancedTests
{
    [Fact]
    public async Task WhenBlockThrowsBlockException_ThenPipelineThrowsAggregate()
    {
        //arrange
        var block = new ThrowingBlock("block-1");
        var pipeline = block.InitBlock();

        //act
        var action = async () => await pipeline.Run().WaitSeconds(5);

        //assert
        await action.Should().ThrowAsync<AggregateException>();
    }

    [Fact]
    public async Task WhenBlockThrowsNonBlockException_ThenPipelineThrowsAggregate()
    {
        //arrange
        var block = new NonBlockThrowingBlock();
        var pipeline = block.InitBlock();

        //act
        var action = async () => await pipeline.Run().WaitSeconds(5);

        //assert
        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public void WhenFlowRunResultCreated_ThenPropertiesAreSet()
    {
        //arrange
        // (none needed)

        //act
        var result = new FlowRunResult(new FlowName("test"), "output-value");

        //assert
        result.FlowName.Should().Be(new FlowName("test"));
        result.Output.Should().Be("output-value");
    }

    [Fact]
    public void WhenPipelineCreationOptionsHaveSubFlow_ThenIsSubFlowIsTrue()
    {
        //arrange
        var options = new PipelineCreationOptions { IsSubFlow = true, RunId = Guid.NewGuid() };

        //act
        // (property access)

        //assert
        options.IsSubFlow.Should().BeTrue();
    }

    [Fact]
    public void WhenPipelineCreationOptionsWithDefaults_ThenValuesAreDefault()
    {
        //arrange
        // (none needed)

        //act
        var options = new PipelineCreationOptions();

        //assert
        options.IsSubFlow.Should().BeFalse();
        options.ShowLogo.Should().BeFalse();
    }

    // --- Test doubles ---

    private class ThrowingBlock(string id) : NoInNoOutPipelineBlock
    {
        public ThrowingBlock() : this("test") { }
        public override IInputInfo Inputs => null!;
        public override Task RunForInput() => throw new BlockOperationException(id, new Exception("inner"));
    }

    private class NonBlockThrowingBlock : NoInNoOutPipelineBlock
    {
        public override IInputInfo Inputs => null!;
        public override Task RunForInput() => throw new InvalidOperationException("not a block exception");
    }

}
