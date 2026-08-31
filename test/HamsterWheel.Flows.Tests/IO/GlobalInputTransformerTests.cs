using FluentAssertions;
using HamsterWheel.Flows.Blocks.Os;
using HamsterWheel.Flows.IO;
using HamsterWheel.Flows.Monitoring;

namespace HamsterWheel.Flows.Tests.IO;

public class GlobalInputTransformerTests
{
    [Fact]
    public void WhenNothingRegistered_GetTransformationReturnsNull()
    {
        //arrange
        var sut = new TestGlobalInputTransformer();

        //act
        var transformation = sut.GetTransformation<OsCommandBlock>();

        //assert
        transformation.Should().BeNull();
    }

    [Fact]
    public void WhenTransformationRegistered_GetTransformationReturnsIt()
    {
        //arrange
        var sut = new TestGlobalInputTransformer();
        sut.RegisterForBlock<OsCommandBlock, OsCommandInputTaskSource, OsCommandInput>((logger, input) => input);

        //act
        var transformation = sut.GetTransformation<OsCommandBlock>();
        var result = transformation!(new PipelineLogger(), null);

        //assert
        transformation.Should().NotBeNull();
        result.Should().BeNull();
    }

    private class TestGlobalInputTransformer : GlobalInputTransformer
    {
    }
}
