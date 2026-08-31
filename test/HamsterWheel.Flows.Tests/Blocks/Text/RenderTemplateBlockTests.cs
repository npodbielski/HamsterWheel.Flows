using FluentAssertions;
using HamsterWheel.Flows.Blocks.Text;
using HamsterWheel.Flows.Templates;
using HamsterWheel.Flows.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace HamsterWheel.Flows.Tests.Blocks.Text;

public class RenderTemplateBlockTests
{
    [Fact]
    public async Task WhenModelPassedAsConst_ThenItRendersUsingModel()
    {
        //arrange
        var expected = "The value of model is test";
        var renderingService = Substitute.For<IRenderingService>();
        renderingService.Render(Arg.Any<string>(), Arg.Any<Dictionary<string, object?>>()).Returns(expected);
        var sut = new RenderTemplateBlock();
        var pipeline = sut.InitBlock(configureServices: s => s.AddScoped<IRenderingService>(_ => renderingService));
        sut.Inputs.Model.Const = new { Name = "test" };
        sut.Inputs.Template.Const = "The value of model is {{ model.name }}";

        //act
        await pipeline.Run().WaitSeconds(1);
        var actual = await sut.Result.SingleValue;

        //assert
        actual.Should().Be(expected);
    }

    [Fact]
    public async Task WhenModelPassedAsLink_ThenItRendersUsingModel()
    {
        //arrange
        var expected = "The value of model is test2";
        var renderingService = Substitute.For<IRenderingService>();
        renderingService.Render(Arg.Any<string>(), Arg.Any<Dictionary<string, object?>>()).Returns(expected);
        var sut = new RenderTemplateBlock();
        var pipeline = sut.InitBlock(configureServices: s => s.AddScoped<IRenderingService>(_ => renderingService));
        sut.Inputs.Model.SetSource(Task.FromResult((IEnumerable<object>) [new { Name = "test2" }]));
        sut.Inputs.Template.Const = "The value of model is {{model.name}}";

        //act
        var enumerator = sut.Result.AsEnumerable();
        await pipeline.Run().WaitSeconds(1);
        var actual = await enumerator.FirstOrDefaultAsync().AsTask().WaitSeconds(1);

        //assert
        actual.Should().Be(expected);
    }
}