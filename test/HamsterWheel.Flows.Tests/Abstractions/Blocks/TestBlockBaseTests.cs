using FluentAssertions;
using HamsterWheel.Flows.Blocks.Os.IO;

namespace HamsterWheel.Flows.Tests.Abstractions.Blocks;

public class TestBlockBaseTests
{
    [Fact]
    public async Task WhenInputFails_ThenBlockFails()
    {
        //arrange
        var sut = new JoinPathsBlock();
        var pipeline = sut.InitBlock();
        sut.Inputs.First.SetSource(Task.FromException<string?>(new Exception()));
        sut.Inputs.Second.Const = "flow";
        pipeline.FinishPipelineInit();

        //act
        var action =  async () => await pipeline.Run();
        await action.Should().ThrowAsync<Exception>();

        //assert
        sut.Completion.Status.Should().Be(TaskStatus.Faulted);
    }

    [Fact]
    public async Task WhenConditionFails_ThenBlockFails()
    {
        //arrange
        var sut = new JoinPathsBlock();
        var pipeline = sut.InitBlock();
        sut.Condition = Task.FromException<bool>(new Exception());
        sut.Inputs.Second.Const = "flow";
        sut.Inputs.First.Const = "/tmp";
        pipeline.FinishPipelineInit();

        //act
        var action =  async () => await pipeline.Run();
        await action.Should().ThrowAsync<Exception>();

        //assert
        sut.Completion.Status.Should().Be(TaskStatus.Faulted);
    }

    [Fact]
    public async Task WhenTriggerFails_ThenBlockFails()
    {
        //arrange
        var sut = new JoinPathsBlock();
        var pipeline = sut.InitBlock();
        sut.TriggerAfter(Task.FromException<bool>(new Exception()));
        sut.Inputs.Second.Const = "flow";
        sut.Inputs.First.Const = "/tmp";
        pipeline.FinishPipelineInit();

        //act
        var action =  async () => await pipeline.Run();
        await action.Should().ThrowAsync<Exception>();

        //assert
        sut.Completion.Status.Should().Be(TaskStatus.Faulted);
    }
}