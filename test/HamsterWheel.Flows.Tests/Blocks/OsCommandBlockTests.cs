using FluentAssertions;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Os;
using HamsterWheel.Flows.Tests.Utils;

namespace HamsterWheel.Flows.Tests.Blocks;

public class OsCommandBlockTests
{
    [Fact]
    public async Task WhenCommandSucceeds_ThenReturnsTrue()
    {
        //arrange
        var block = new OsCommandBlock();
        var pipeline = block.InitBlock();
        block.Inputs.Command.Const = "echo";
        block.Inputs.Arguments.Const = "hello";
        block.Inputs.WorkingDir.Const = Directory.GetCurrentDirectory();

        //act
        await pipeline.Run().WaitSeconds(10);
        var result = await block.Result.SingleValue.WaitSeconds(10);

        //assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task WhenCommandFails_ThenReturnsFalse()
    {
        //arrange
        var block = new OsCommandBlock();
        var pipeline = block.InitBlock();
        block.Inputs.Command.Const = "sh";
        //double quotes are stripped by .NET argument parsing; single quotes would be passed to the shell literally
        block.Inputs.Arguments.Const = "-c \"exit 1\"";
        block.Inputs.WorkingDir.Const = Directory.GetCurrentDirectory();

        //act
        await pipeline.Run().WaitSeconds(10);
        var result = await block.Result.SingleValue.WaitSeconds(10);

        //assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task WhenRetriesZero_ThenRunsWithoutRetry()
    {
        //arrange
        var block = new OsCommandBlock();
        var pipeline = block.InitBlock();
        block.Inputs.Command.Const = "echo";
        block.Inputs.Arguments.Const = "hello";
        block.Inputs.WorkingDir.Const = Directory.GetCurrentDirectory();
        block.Inputs.Retries.Const = 0;

        //act
        await pipeline.Run().WaitSeconds(10);
        var result = await block.Result.SingleValue.WaitSeconds(10);

        //assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task WhenCommandSucceedsWithRetries_ThenReturnsTrue()
    {
        //arrange
        var block = new OsCommandBlock();
        var pipeline = block.InitBlock();
        block.Inputs.Command.Const = "echo";
        block.Inputs.Arguments.Const = "hello";
        block.Inputs.WorkingDir.Const = Directory.GetCurrentDirectory();
        block.Inputs.Retries.Const = 3;

        //act
        await pipeline.Run().WaitSeconds(10);
        var result = await block.Result.SingleValue.WaitSeconds(10);

        //assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task WhenCommandFailsWithRetries_ThenReturnsFalse()
    {
        //arrange
        var block = new OsCommandBlock();
        var pipeline = block.InitBlock();
        block.Inputs.Command.Const = "sh";
        block.Inputs.Arguments.Const = "-c \"exit 1\"";
        block.Inputs.WorkingDir.Const = Directory.GetCurrentDirectory();
        block.Inputs.Retries.Const = 2;

        //act
        await pipeline.Run().WaitSeconds(10);
        var result = await block.Result.SingleValue.WaitSeconds(10);

        //assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task WhenCommandTimesOut_ThenRunThrowsTimeoutException()
    {
        //arrange
        var block = new OsCommandBlock();
        var pipeline = block.InitBlock();
        block.Inputs.Command.Const = "sleep";
        block.Inputs.Arguments.Const = "2";
        block.Inputs.WorkingDir.Const = Directory.GetCurrentDirectory();
        block.Inputs.WaitForSeconds.Const = 1;

        //act
        var exception = await Record.ExceptionAsync(() => pipeline.Run());

        //assert
        //the pipeline rewraps non-BlockExceptions (the block fault is a BlockOperationException wrapping this)
        exception.Should().BeOfType<AggregateException>();
        ((AggregateException)exception!).InnerExceptions.Should().ContainSingle()
            .Which.Should().BeOfType<TimeoutException>();
    }

    [Fact]
    public async Task WhenCommandTimesOutOnLastRetry_ThenRunThrowsTimeoutException()
    {
        //arrange
        //earlier retries swallow the timeout (propagateOutput=false); the last retry (Retries==1) rethrows
        var block = new OsCommandBlock();
        var pipeline = block.InitBlock();
        block.Inputs.Command.Const = "sleep";
        block.Inputs.Arguments.Const = "2";
        block.Inputs.WorkingDir.Const = Directory.GetCurrentDirectory();
        block.Inputs.WaitForSeconds.Const = 1;
        block.Inputs.Retries.Const = 2;

        //act
        var exception = await Record.ExceptionAsync(() => pipeline.Run());

        //assert
        exception.Should().BeOfType<AggregateException>();
        ((AggregateException)exception!).InnerExceptions.Should().ContainSingle()
            .Which.Should().BeOfType<TimeoutException>();
    }

    [Fact]
    public async Task WhenRunCancelled_ThenCompletionFaultsWithOperationCanceledException()
    {
        //arrange
        var block = new OsCommandBlock();
        var pipeline = block.InitBlock();
        block.Inputs.Command.Const = "sleep";
        block.Inputs.Arguments.Const = "3";
        block.Inputs.WorkingDir.Const = Directory.GetCurrentDirectory();
        using var cts = new CancellationTokenSource();

        //act
        var runTask = pipeline.Run(cts.Token);
        await Task.Delay(300).WaitSeconds(5);
        cts.Cancel();
        await block.Completion.WaitSeconds(10);

        //assert
        block.Completion.IsFaulted.Should().BeTrue();
        var blockException = block.Completion.Exception!.InnerExceptions.Should().ContainSingle().Subject;
        blockException.Should().BeOfType<BlockOperationException>();
        blockException.InnerException.Should().BeOfType<TaskCanceledException>();
        try
        {
            //observe the pipeline task so its (cancellation) exception is not left unobserved
            await runTask.WaitSeconds(10);
        }
        catch
        {
            //the pipeline surfaces the cancellation as its own exception; the block fault is asserted above
        }
    }
}
