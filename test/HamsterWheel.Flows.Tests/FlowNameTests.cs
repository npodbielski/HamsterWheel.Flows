using FluentAssertions;
using HamsterWheel.Flows;

namespace HamsterWheel.Flows.Tests;

public class FlowNameTests
{
    [Fact]
    public void WhenNoPrefix_ToStringReturnsName()
    {
        //arrange
        var sut = new FlowName("my-flow");

        //act
        var result = sut.ToString();

        //assert
        result.Should().Be("my-flow");
    }

    [Fact]
    public void WhenPrefixSet_ToStringReturnsPrefixAndName()
    {
        //arrange
        var sut = new FlowName("my-flow", "ext");

        //act
        var result = sut.ToString();

        //assert
        result.Should().Be("ext:my-flow");
    }

    [Fact]
    public void WhenNormalizeFlowNameEndsWithFlow_ReturnsPascalCasedName()
    {
        //arrange
        // (none needed)

        //act
        var result = FlowName.NormalizeFlowName("myFlow");

        //assert
        result.Should().Be("MyFlow");
    }

    [Fact]
    public void WhenNormalizeFlowNameDoesNotEndWithFlow_AppendsFlowSuffix()
    {
        //arrange
        // (none needed)

        //act
        var result = FlowName.NormalizeFlowName("myflow");

        //assert
        result.Should().Be("MyflowFlow");
    }

    [Fact]
    public void WhenFromStringWithoutSeparator_ReturnsNameOnly()
    {
        //arrange
        // (none needed)

        //act
        var result = FlowName.FromString("my-flow");

        //assert
        result.Name.Should().Be("my-flow");
        result.Prefix.Should().BeNull();
    }

    [Fact]
    public void WhenFromStringWithThreeChunks_ReturnsNameAndPrefix()
    {
        //arrange
        // (none needed)

        //act
        var result = FlowName.FromString("a:b:c");

        //assert
        result.Name.Should().Be("b");
        result.Prefix.Should().Be("c");
    }

    [Fact]
    public void WhenFromStringWithTrailingSeparator_ReturnsOriginalName()
    {
        //arrange
        // (none needed)

        //act
        var result = FlowName.FromString("a:");

        //assert
        result.Name.Should().Be("a:");
        result.Prefix.Should().BeNull();
    }
}
