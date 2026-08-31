using FluentAssertions;
using HamsterWheel.Flows;
using HamsterWheel.Flows.IO;
using HamsterWheel.Flows.Pipelines;
using HamsterWheel.Flows.Runner;
using HamsterWheel.Flows.Templates;
using HamsterWheel.Flows.Data.Serialization;
using NSubstitute;

namespace HamsterWheel.Flows.Tests.Infrastructure;

public class FlowApplierTests
{
    [Fact]
    public async Task WhenApplyWithMatchingInputType_ThenFlowIsAppliedWithOriginalInput()
    {
        //arrange
        var (applier, coordinator, _, pipeline, flow) = CreateApplier(out var inputBag);
        flow.HaveInput.Returns(true);
        flow.InputType.Returns(typeof(string));
        flow.ApplyToPipeline(pipeline, inputBag).Returns(Task.CompletedTask);

        //act
        await applier.Apply(pipeline, flow, "my input");

        //assert
        coordinator.Received(1).Authorize(flow);
        pipeline.Received(1).AttachFlowContext(Arg.Any<IFlowContext>());
        flow.Received(1).ApplyToPipeline(pipeline, inputBag);
    }

    [Fact]
    public async Task WhenApplyWithNoInput_ThenContextInputIsNullAndConverterIsNotCalled()
    {
        //arrange
        var (applier, coordinator, converter, pipeline, flow) = CreateApplier(out var inputBag);
        flow.HaveInput.Returns(false);
        flow.ApplyToPipeline(pipeline, inputBag).Returns(Task.CompletedTask);
        IFlowContext? capturedContext = null;
        pipeline.AttachFlowContext(Arg.Do<IFlowContext>(c => capturedContext = c));

        //act
        await applier.Apply(pipeline, flow, null);

        //assert
        capturedContext.Should().NotBeNull();
        capturedContext!.Input.Should().BeNull();
        coordinator.Received(1).Authorize(flow);
        converter.DidNotReceive().ConvertTo(Arg.Any<Type?>(), Arg.Any<object?>());
    }

    [Fact]
    public async Task WhenApplyWithMismatchedInputType_ThenConvertedValueIsPassedToContext()
    {
        //arrange
        var (applier, coordinator, converter, pipeline, flow) = CreateApplier(out var inputBag);
        flow.HaveInput.Returns(true);
        flow.InputType.Returns(typeof(string));
        flow.ApplyToPipeline(pipeline, inputBag).Returns(Task.CompletedTask);
        converter.ConvertTo(typeof(string), Arg.Any<object>()).Returns("converted");
        IFlowContext? capturedContext = null;
        pipeline.AttachFlowContext(Arg.Do<IFlowContext>(c => capturedContext = c));

        //act
        await applier.Apply(pipeline, flow, 42);

        //assert
        converter.Received(1).ConvertTo(typeof(string), 42);
        capturedContext.Should().NotBeNull();
        capturedContext!.Input.Should().Be("converted");
    }

    [Fact]
    public async Task WhenConverterThrows_ThenExceptionIsPropagated()
    {
        //arrange
        var (applier, coordinator, converter, pipeline, flow) = CreateApplier(out var inputBag);
        flow.HaveInput.Returns(true);
        flow.InputType.Returns(typeof(string));
        converter.When(c => { _ = c.ConvertTo(typeof(string), Arg.Any<object>()); })
            .Throw(new InvalidOperationException("conversion failed"));

        //act
        var action = async () => await applier.Apply(pipeline, flow, 42);

        //assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*conversion failed*");
    }

    [Fact]
    public async Task WhenInputImplementsDataWithValidator_ThenValidateIsCalled()
    {
        //arrange
        var (applier, coordinator, _, pipeline, flow) = CreateApplier(out var inputBag);
        var input = new ValidatingInput();
        flow.HaveInput.Returns(true);
        flow.InputType.Returns(typeof(ValidatingInput));
        flow.ApplyToPipeline(pipeline, inputBag).Returns(Task.CompletedTask);

        //act
        await applier.Apply(pipeline, flow, input);

        //assert
        input.WasValidated.Should().BeTrue();
    }

    [Fact]
    public async Task WhenApplyWithNullInputAndFlowRequiresInput_ThenThrows()
    {
        //arrange
        var (applier, coordinator, _, pipeline, flow) = CreateApplier(out var inputBag);
        flow.HaveInput.Returns(true);
        flow.InputType.Returns(typeof(string));

        //act
        var action = async () => await applier.Apply(pipeline, flow, null);

        //assert
        await action.Should().ThrowAsync<FlowNoInputException>();
    }

    private static (FlowApplier Applier, IFlowCoordinator Coordinator,
        HamsterWheel.HLinq.Data.Converters.IDefaultConverter Converter, IPipeline Pipeline, IFlow Flow)
        CreateApplier(out FlowGlobalInputBag inputBag)
    {
        var coordinator = Substitute.For<IFlowCoordinator>();
        var converter = Substitute.For<HamsterWheel.HLinq.Data.Converters.IDefaultConverter>();
        inputBag = new FlowGlobalInputBag(
            Substitute.For<IRenderingService>(),
            Substitute.For<ISerializer>(),
            null);
        var applier = new FlowApplier(coordinator, converter, inputBag);
        var pipeline = Substitute.For<IPipeline>();
        var flow = Substitute.For<IFlow>();
        return (applier, coordinator, converter, pipeline, flow);
    }

    private class ValidatingInput : IDataWithValidator
    {
        public bool WasValidated { get; private set; }

        public void Validate() => WasValidated = true;
    }
}
