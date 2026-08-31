using FluentAssertions;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Text;
using HamsterWheel.Flows.Tests.Utils;
using HamsterWheel.Flows.Templates;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace HamsterWheel.Flows.Tests.Blocks;

public class RenderTemplateBlockTests
{
    [Fact]
    public async Task WhenTemplateIsRendered_ThenOutputContainsRenderedText()
    {
        //arrange
        var renderingService = Substitute.For<IRenderingService>();
        renderingService.Render(Arg.Any<string>(), Arg.Any<Dictionary<string, object?>>(), Arg.Any<CancellationToken>())
            .Returns("Hello World");
        var block = new RenderTemplateBlock();
        var pipeline = block.InitBlock(configureServices: s =>
            s.AddSingleton<IRenderingService>(renderingService));
        block.Inputs.Template.Const = "Hello {{input}}";

        //act
        await pipeline.Run().WaitSeconds(5);
        var actual = await block.Result.SingleValue.WaitSeconds(5);

        //assert
        actual.Should().Be("Hello World");
    }
}
