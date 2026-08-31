using FluentAssertions;
using HamsterWheel.Flows.Templates;

namespace HamsterWheel.Flows.Tests.Templates;

public class SimpleRenderingServiceUnitTests
{
    private readonly SimpleRenderingService _sut = new();

    [Fact]
    public async Task Render_WhenJustSimpleString_ThenRendersAsIs()
    {
        //arrange
        const string expected = "Hello World!";

        //act
        var actual = await _sut.Render("Hello World!", []);

        //assert
        actual.Should().Be(expected);
    }

    [Fact]
    public async Task Render_WhenSingleModelInjectDirective_ThenAddsModel()
    {
        //arrange
        const string expected = "Hello World!";

        //act
        var actual = await _sut.Render("Hello {{ world_name }}!", new () {{"world_name", "World"}});

        //assert
        actual.Should().Be(expected);
    }

    [Fact(Skip = "HLinq select[x.name] syntax not supported in current package version")]
    public async Task Render_WhenSingleModelPropertyInjectDirective_ThenAddsModel()
    {
        //arrange
        const string expected = "Hello World!";

        //act
        var actual = await _sut.Render("Hello {{ world.name }}!", new () {{"world", new { Name = "World" } }});

        //assert
        actual.Should().Be(expected);
    }

    [Fact(Skip = "HLinq select[x.name] syntax not supported in current package version")]
    public async Task Render_WhenSingleModelPropertyInjectDirectiveWithConversion_ThenAddsModel()
    {
        //arrange
        const string expected = "Hello 56656!";

        //act
        var actual = await _sut.Render("Hello {{ world.name }}!", new () {{"world", new { Name = 56656 } }});

        //assert
        actual.Should().Be(expected);
    }
}