using FluentAssertions;
using HamsterWheel.Flows.Blocks.Os.IO;
using HamsterWheel.Flows.Tests.Utils;

namespace HamsterWheel.Flows.Tests.Blocks.Os.IO;

public class JoinPathBlockTests
{
    [Fact]
    public async Task Run_WhenOnlyConstInputs_SaveFile()
    {
        //arrange
        var sut = new JoinPathsBlock();
        var pipeline = sut.InitBlock();
        sut.Inputs.First.Const = "/tmp";
        sut.Inputs.Second.Const = "flow";

        //act
        await pipeline.Run().WaitSeconds(1);

        //assert
        (await sut.Result.SingleValue).Should().BeEquivalentTo("/tmp/flow");
    }
}