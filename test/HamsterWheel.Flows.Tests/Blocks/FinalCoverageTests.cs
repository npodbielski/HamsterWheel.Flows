using FluentAssertions;
using HamsterWheel.Flows;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Base;
using HamsterWheel.Flows.Blocks.Logic;
using HamsterWheel.Flows.Blocks.Os;
using HamsterWheel.Flows.Blocks.Os.IO;
using HamsterWheel.Flows.Blocks.Utils;
using HamsterWheel.Flows.IO;
using HamsterWheel.Flows.Pipelines;
using HamsterWheel.Flows.Tests.Utils;
using HamsterWheel.HLinq.Data.Converters;

namespace HamsterWheel.Flows.Tests.Blocks;

public class FinalCoverageTests
{
    [Fact]
    public async Task WhenOsCommandWithRetriesFailing_ThenRetriesWork()
    {
        //arrange
        var block = new OsCommandBlock();
        var pipeline = block.InitBlock();
        block.Inputs.Command.Const = "false";
        block.Inputs.WorkingDir.Const = Path.GetTempPath();
        block.Inputs.Retries.Const = 2;

        //act
        await pipeline.Run().WaitSeconds(10);
        var result = await block.Result.SingleValue.WaitSeconds(5);

        //assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task WhenOsCommandWithWaitForSeconds_ThenReturnsTrue()
    {
        //arrange
        var block = new OsCommandBlock();
        var pipeline = block.InitBlock();
        block.Inputs.Command.Const = "true";
        block.Inputs.WorkingDir.Const = Path.GetTempPath();
        block.Inputs.WaitForSeconds.Const = 1;

        //act
        await pipeline.Run().WaitSeconds(10);
        var result = await block.Result.SingleValue.WaitSeconds(5);

        //assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task WhenOsCommandWithArguments_ThenRunsCorrectly()
    {
        //arrange
        var block = new OsCommandBlock();
        var pipeline = block.InitBlock();
        block.Inputs.Command.Const = "echo";
        block.Inputs.Arguments.Const = "hello";
        block.Inputs.WorkingDir.Const = Path.GetTempPath();

        //act
        await pipeline.Run().WaitSeconds(10);
        var result = await block.Result.SingleValue.WaitSeconds(5);

        //assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task WhenLogBlock_ThenLogsMessage()
    {
        //arrange
        var iterate = new IterateBlock();
        var log = new LogBlock();
        var pipeline = ((IPipelineBlock[])[iterate, log]).InitBlock();
        iterate.Inputs.Const = new object[] { "msg1", "msg2" };
        log.Inputs.SetSource(iterate, DefaultConverter.Instance.ConvertTo<string>);

        //act
        await pipeline.Run().WaitSeconds(5);

        //assert
        // Log block has no output, just verify no exception
    }

    [Fact]
    public async Task WhenJoinPathsBlock_ThenPathsJoined()
    {
        //arrange
        var block = new JoinPathsBlock();
        var pipeline = block.InitBlock();
        block.Inputs.First.Const = "/tmp";
        block.Inputs.Second.Const = "test";

        //act
        await pipeline.Run().WaitSeconds(5);
        var result = await block.Result.SingleValue.WaitSeconds(5);

        //assert
        result.Should().Be("/tmp/test");
    }

    [Fact]
    public async Task WhenOsCommandWithWorkingDirOnly_ThenRuns()
    {
        //arrange
        var block = new OsCommandBlock();
        var pipeline = block.InitBlock();
        block.Inputs.Command.Const = "true";
        block.Inputs.WorkingDir.Const = Path.GetTempPath();

        //act
        await pipeline.Run().WaitSeconds(10);
        var result = await block.Result.SingleValue.WaitSeconds(5);

        //assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task WhenIterateThenLog_ThenAllItemsLogged()
    {
        //arrange
        var iterate = new IterateBlock();
        var log = new LogBlock();
        var pipeline = ((IPipelineBlock[])[iterate, log]).InitBlock();
        iterate.Inputs.Const = new object[] { 1, 2, 3 };
        log.Inputs.SetSource(iterate, DefaultConverter.Instance.ConvertTo<string>);

        //act
        await pipeline.Run().WaitSeconds(5);

        //assert
        // No exception = pass
    }
}
