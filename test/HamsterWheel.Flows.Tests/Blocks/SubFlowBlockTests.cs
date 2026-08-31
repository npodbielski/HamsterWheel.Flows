using FluentAssertions;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Runner;
using HamsterWheel.Flows.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace HamsterWheel.Flows.Tests.Blocks;

public class SubFlowBlockTests
{
    [Fact]
    public async Task WhenSubFlowRuns_ThenFlowRunnerIsCalled()
    {
        //arrange
        var flowRunner = Substitute.For<IFlowRunner>();
        var expectedResult = new FlowRunResult(new FlowName("sub-flow"), "sub-result");
        IFlowRunResult resultRef = expectedResult;
        flowRunner.RunAsync(Arg.Any<IScheduledFlowData>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(resultRef));

        var block = new SubFlowBlock();
        var pipeline = block.InitBlock(configureServices: s =>
            s.AddSingleton<IFlowRunner>(flowRunner));
        block.Inputs.FlowName.Const = "sub-flow";

        //act
        await pipeline.Run().WaitSeconds(5);
        var result = await block.Result.SingleValue.WaitSeconds(5);

        //assert
        result.Should().BeSameAs(expectedResult);
        await flowRunner.Received(1).RunAsync(Arg.Any<IScheduledFlowData>(), Arg.Any<CancellationToken>());
    }
}
