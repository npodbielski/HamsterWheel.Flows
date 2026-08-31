using System.Diagnostics;
using FluentAssertions;
using HamsterWheel.Flows.Tests.Utils;

namespace HamsterWheel.Flows.Tests;

public class ProcessExtensionsTests
{
    [Fact]
    public async Task WhenProcessExited_TryReadExitCodeReturnsItsCode()
    {
        //arrange
        //double quotes are stripped by .NET argument parsing; single quotes would be passed to the shell literally
        var process = Start("sh", "-c \"exit 3\"");
        await process.WaitForExitAsync().WaitSeconds(10);

        //act
        var exitCode = process.TryReadExitCode();

        //assert
        exitCode.Should().Be(3);
    }

    [Fact]
    public async Task WhenProcessStillRunning_TryReadExitCodeReturnsNull()
    {
        //arrange
        var process = Start("sleep", "10");

        //act
        var exitCode = process.TryReadExitCode();

        //assert
        exitCode.Should().BeNull();
        process.Kill(true);
        await process.WaitForExitAsync().WaitSeconds(10);
    }

    [Fact]
    public async Task WhenProcessExitsBeforeTimeout_ReadAndWaitReturnsCompletedExitTask()
    {
        //arrange
        var process = Start("echo", "hello");
        var logs = new List<string>();

        //act
        var exitTask = await process.ReadAndWait(logs.Add, CancellationToken.None, 5).WaitSeconds(10);

        //assert
        exitTask.IsCompletedSuccessfully.Should().BeTrue();
        await exitTask.WaitSeconds(10);
    }

    [Fact]
    public async Task WhenProcessRunsLongerThanFirstSecond_ReadAndWaitLogsOutputAndReturnsExitTask()
    {
        //arrange
        var process = Start("sh", "-c \"echo hello; sleep 3\"");
        var logs = new List<string>();

        //act
        var exitTask = await process.ReadAndWait(logs.Add, CancellationToken.None, 5).WaitSeconds(15);

        //assert
        exitTask.IsCompletedSuccessfully.Should().BeTrue();
        logs.Should().Contain(log => log.Contains("hello"));
    }

    [Fact]
    public async Task WhenProcessDoesNotExitBeforeTimeout_ReadAndWaitThrowsTimeoutException()
    {
        //arrange
        var process = Start("sh", "-c \"sleep 3\"");

        //act + assert
        var exception = await Record.ExceptionAsync(
            () => process.ReadAndWait(_ => { }, CancellationToken.None, 1));
        exception.Should().BeOfType<TimeoutException>();
    }

    [Fact]
    public async Task WhenTokenCancelled_ReadAndWaitThrowsOperationCanceledException()
    {
        //arrange
        var process = Start("sleep", "10");
        using var cts = new CancellationTokenSource();

        //act
        var task = process.ReadAndWait(_ => { }, cts.Token, 5);
        await Task.Delay(300).WaitSeconds(5);
        cts.Cancel();

        //assert
        (await Record.ExceptionAsync(() => task)).Should().BeOfType<TaskCanceledException>();
        process.Kill(true);
        await process.WaitForExitAsync().WaitSeconds(10);
    }

    [Fact]
    public async Task WhenTokenAlreadyCancelled_ReadAndWaitThrowsTaskCanceledException()
    {
        //arrange
        var process = Start("sleep", "10");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        //act
        var task = process.ReadAndWait(_ => { }, cts.Token, 5);

        //assert
        (await Record.ExceptionAsync(() => task)).Should().BeOfType<TaskCanceledException>();
        process.Kill(true);
        await process.WaitForExitAsync().WaitSeconds(10);
    }

    private static Process Start(string fileName, string arguments)
    {
        var process = new Process
        {
            StartInfo =
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        process.Start();
        return process;
    }
}
