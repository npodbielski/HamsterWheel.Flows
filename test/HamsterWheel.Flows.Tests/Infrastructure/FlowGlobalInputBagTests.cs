using FluentAssertions;
using HamsterWheel.Flows;
using HamsterWheel.Flows.Pipelines;
using HamsterWheel.Flows.Data.Serialization;
using HamsterWheel.Flows.Templates;
using NSubstitute;

namespace HamsterWheel.Flows.Tests.Infrastructure;

public class FlowGlobalInputBagTests
{
    [Fact]
    public void WhenFromContext_ThenFlowAndInputAreSet()
    {
        //arrange
        var bag = CreateBag();
        var flow = Substitute.For<IFlow>();
        var context = Substitute.For<IFlowContext>();
        context.Flow.Returns(flow);
        context.Input.Returns("input-data");

        //act
        bag.FromContext(context);

        //assert
        bag.InputAsObject.Should().Be("input-data");
    }

    [Fact]
    public void WhenInputAvailableAndRender_ThenTemplateHasInput()
    {
        //arrange
        var renderingService = Substitute.For<IRenderingService>();
        var bag = new FlowGlobalInputBag(renderingService, new BasicJsonSerializer(), null);
        var flow = Substitute.For<IFlow>();
        flow.HaveInput.Returns(true);
        var context = Substitute.For<IFlowContext>();
        context.Flow.Returns(flow);
        context.Input.Returns("my-input");
        bag.FromContext(context);

        //act
        var result = bag.Render("{{input}}").Result;

        //assert
        renderingService.Received(1).Render("{{input}}", Arg.Is<Dictionary<string, object?>>(d => d["input"] == "my-input"));
    }

    [Fact]
    public void WhenSerializeWithNonString_ThenUsesSerializer()
    {
        //arrange
        var bag = CreateBag();

        //act
        var result = bag.Serialize(new { A = 1 });

        //assert
        result.Should().Contain("\"A\":1");
    }

    [Fact]
    public void WhenSerializeWithString_ThenReturnsAsIs()
    {
        //arrange
        var bag = CreateBag();

        //act
        var result = bag.Serialize("plain string");

        //assert
        result.Should().Be("plain string");
    }

    [Fact]
    public void WhenExtraInputBagProvided_ThenBagIsUsed()
    {
        //arrange
        var extraBag = Substitute.For<IExtraInputBag>();
        var dict = new Dictionary<string, object> { ["key"] = "value" };
        extraBag.Bag.Returns(dict);
        var bag = new FlowGlobalInputBag(Substitute.For<IRenderingService>(), new BasicJsonSerializer(), extraBag);

        //act
        // (property access)

        //assert
        bag.Bag.Should().BeSameAs(dict);
    }

    private static FlowGlobalInputBag CreateBag() =>
        new(Substitute.For<IRenderingService>(), new BasicJsonSerializer(), null);
}
