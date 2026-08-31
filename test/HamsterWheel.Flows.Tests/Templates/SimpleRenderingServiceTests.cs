using FluentAssertions;
using HamsterWheel.Flows.Templates;
using NSubstitute;

namespace HamsterWheel.Flows.Tests.Templates;

//HLinq (ExecuteHLinq) keeps shared state, so it races with other test classes calling it in parallel
[Collection("hlinq")]
public class SimpleRenderingServiceTests
{
    [Fact]
    public async Task WhenTemplateHasVariable_ThenItIsReplaced()
    {
        //arrange
        var service = new SimpleRenderingService();
        var models = new Dictionary<string, object?> { ["name"] = "World" };

        //act
        var result = await service.Render("Hello {{name}}!", models);

        //assert
        result.Should().Be("Hello World!");
    }

    [Fact]
    public async Task WhenTemplateHasVariableAtStart_ThenItIsReplaced()
    {
        //arrange
        var service = new SimpleRenderingService();
        var models = new Dictionary<string, object?> { ["name"] = "World" };

        //act
        var result = await service.Render("{{name}}!", models);

        //assert
        result.Should().Be("World!");
    }

    [Fact]
    public async Task WhenTemplateHasNoVariables_ThenReturnsAsIs()
    {
        //arrange
        var service = new SimpleRenderingService();
        var models = new Dictionary<string, object?>();

        //act
        var result = await service.Render("No variables here", models);

        //assert
        result.Should().Be("No variables here");
    }

    [Fact]
    public async Task WhenVariableIsMissing_ThenPlaceholderIsRemoved()
    {
        //arrange
        var service = new SimpleRenderingService();
        var models = new Dictionary<string, object?>();

        //act
        var result = await service.Render("Hello {{missing}}!", models);

        //assert
        result.Should().Be("Hello !");
    }

    [Fact]
    public async Task WhenTemplateIsEmpty_ThenReturnsEmpty()
    {
        //arrange
        var service = new SimpleRenderingService();
        var models = new Dictionary<string, object?>();

        //act
        var result = await service.Render("", models);

        //assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenTemplateHasModelProperty_ThenRendersPropertyWithValue()
    {
        //arrange
        var service = new SimpleRenderingService();
        var models = new Dictionary<string, object?>
        {
            ["user"] = new { Name = "Alice" }
        };

        //act
        var result = await service.Render("Hello {{user.name}}!", models);

        //assert
        result.Should().Be("Hello Alice!");
    }
}
