using System.Collections.Concurrent;
using FluentAssertions;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Monitoring;
using HamsterWheel.Flows.Tests.Utils;

namespace HamsterWheel.Flows.Tests.Blocks;

public class LogBlockTests
{
    [Fact]
    public async Task WhenInputIsString_ThenMessageIsLogged()
    {
        //arrange
        var block = new LogBlock { Id = "log-block" };
        var loggedMessages = new ConcurrentQueue<IFlowLogMessage>();
        var pipeline = block.InitBlock();
        pipeline.Logger.SetSink(m => loggedMessages.Enqueue(m));
        block.Inputs.Const = "Hello from LogBlock";

        //act
        await pipeline.Run().WaitSeconds(5);

        //assert
        loggedMessages.Should().Contain(m => m.Message == "Hello from LogBlock");
    }
}
