using System.Diagnostics;
using FluentAssertions;
using HamsterWheel.Flows;

namespace HamsterWheel.Flows.Tests.Infrastructure;

public class ProcessExtensionsTests
{
    [Fact]
    public void WhenTryReadExitCodeOnCompletedProcess_ThenReturnsCode()
    {
        //arrange
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/true",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };
        process.Start();
        process.WaitForExit();

        //act
        var result = process.TryReadExitCode();

        //assert
        result.Should().Be(0);
        process.Dispose();
    }

    [Fact]
    public void WhenTryReadExitCodeOnFailedProcess_ThenReturnsNonZero()
    {
        //arrange
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/false",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };
        process.Start();
        process.WaitForExit();

        //act
        var result = process.TryReadExitCode();

        //assert
        result.Should().Be(1);
        process.Dispose();
    }
}
