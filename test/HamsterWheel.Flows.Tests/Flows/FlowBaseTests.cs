using FluentAssertions;
using HamsterWheel.Flows;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Logic;
using HamsterWheel.Flows.Pipelines;
using HamsterWheel.Flows.Templates;
using HamsterWheel.Flows.Data.Serialization;
using NSubstitute;

namespace HamsterWheel.Flows.Tests.Flows;

public class FlowBaseTests
{
    [Fact]
    public void WhenFlowIsNoInputNoOutput_ThenPropertiesAreCorrect()
    {
        //arrange
        var flow = new TestNoInputNoOutputFlow();

        //act
        // (property access)

        //assert
        flow.HaveInput.Should().BeFalse();
        flow.HaveOutput.Should().BeFalse();
        flow.InputType.Should().BeNull();
        flow.OutputType.Should().BeNull();
        flow.BuildOutput().Should().BeNull();
        flow.Version.Should().Be("1.0");
        flow.AverageTime.Should().BeNull();
        flow.DoesAllowAnonymousRuns.Should().BeFalse();
        flow.InputSchema.Should().BeNull();
        flow.OutputSchema.Should().BeNull();
    }

    [Fact]
    public async Task WhenApplyToPipeline_ThenBlocksAreAdded()
    {
        //arrange
        var flow = new TestNoInputNoOutputFlow();
        var pipeline = Substitute.For<IPipeline>();
        var inputBag = Substitute.For<IFlowGlobalInputBag>();

        //act
        await flow.ApplyToPipeline(pipeline, inputBag);

        //assert
        pipeline.Received(1).AddBlock(Arg.Any<IPipelineBlock>());
    }

    [Fact]
    public void WhenFlowIsNoInputFlow_ThenHaveInputIsFalse()
    {
        //arrange
        var flow = new TestNoInputFlow();

        //act
        // (property access)

        //assert
        flow.HaveInput.Should().BeFalse();
        flow.HaveOutput.Should().BeTrue();
        flow.InputType.Should().BeNull();
        flow.OutputType.Should().Be<string>();
        flow.Version.Should().Be("1.0");
        flow.AverageTime.Should().BeNull();
        flow.DoesAllowAnonymousRuns.Should().BeFalse();
        flow.InputSchema.Should().BeNull();
        flow.OutputSchema.Should().BeNull();
        flow.BuildOutput().Should().Be("output");
    }

    [Fact]
    public void WhenFlowIsNoOutputFlow_ThenHaveOutputIsFalse()
    {
        //arrange
        var flow = new TestNoOutputFlow();

        //act
        // (property access)

        //assert
        flow.HaveInput.Should().BeTrue();
        flow.HaveOutput.Should().BeFalse();
        flow.OutputType.Should().BeNull();
        flow.InputType.Should().Be<TestNoOutputFlow.InputDto>();
        flow.Version.Should().Be("1.0");
        flow.AverageTime.Should().BeNull();
        flow.DoesAllowAnonymousRuns.Should().BeFalse();
        flow.InputSchema.Should().BeEmpty();
        flow.OutputSchema.Should().BeNull();
        flow.BuildOutput().Should().BeNull();
    }

    [Fact]
    public void WhenFlowBasePropertiesAccessed_ThenTypesAndOutputAreCorrect()
    {
        //arrange
        var flow = new TestFlow();

        //act
        // (property access)

        //assert
        flow.InputType.Should().Be<string>();
        flow.OutputType.Should().Be<int>();
        flow.HaveInput.Should().BeTrue();
        flow.HaveOutput.Should().BeTrue();
        flow.AverageTime.Should().BeNull();
        flow.DoesAllowAnonymousRuns.Should().BeFalse();
        flow.InputSchema.Should().BeEmpty();
        flow.OutputSchema.Should().BeEmpty();
        flow.BuildOutput().Should().Be(42);
    }

    [Fact]
    public async Task WhenFlowBaseApplyToPipelineWithMatchingInput_ThenTypedInputIsPassedToImpl()
    {
        //arrange
        var flow = new TestFlow();
        var pipeline = Substitute.For<IPipeline>();
        var inputBag = CreateInputBag("my-input");

        //act
        await flow.ApplyToPipeline(pipeline, inputBag);

        //assert
        flow.ReceivedInput.Should().Be("my-input");
    }

    [Fact]
    public async Task WhenNoInputFlowApplyToPipeline_ThenImplIsCalledWithUntypedBag()
    {
        //arrange
        var flow = new TestNoInputFlow();
        var pipeline = Substitute.For<IPipeline>();
        var inputBag = Substitute.For<IFlowGlobalInputBag>();

        //act
        await flow.ApplyToPipeline(pipeline, inputBag);

        //assert
        flow.WasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task WhenNoOutputFlowApplyToPipeline_ThenTypedInputIsPassedToImpl()
    {
        //arrange
        var flow = new TestNoOutputFlow();
        var pipeline = Substitute.For<IPipeline>();
        var inputDto = new TestNoOutputFlow.InputDto();
        var inputBag = CreateInputBag(inputDto);

        //act
        await flow.ApplyToPipeline(pipeline, inputBag);

        //assert
        flow.ReceivedInput.Should().BeSameAs(inputDto);
    }

    [Fact]
    public void WhenFlowInputValuesInputIsNull_ThenReturnsNull()
    {
        //arrange
        var typed = FlowInputValues<string>.From(CreateInputBag(null));

        //act
        var input = typed.Input;

        //assert
        input.Should().BeNull();
    }

    [Fact]
    public void WhenFlowInputValuesInputMatches_ThenReturnsInput()
    {
        //arrange
        var typed = FlowInputValues<string>.From(CreateInputBag("abc"));

        //act
        var input = typed.Input;

        //assert
        input.Should().Be("abc");
    }

    [Fact]
    public void WhenFlowInputValuesInputTypeMismatch_ThenThrowsMismatchedFlowInputTypeException()
    {
        //arrange
        var typed = FlowInputValues<string>.From(CreateInputBag(123));

        //act
        var action = () => typed.Input;

        //assert
        action.Should().Throw<MismatchedFlowInputTypeException>();
    }

    [Fact]
    public async Task WhenFlowInputValuesRender_ThenDelegatesToUntypedBag()
    {
        //arrange
        var rendering = Substitute.For<IRenderingService>();
        rendering.Render("tpl", Arg.Any<Dictionary<string, object?>>()).Returns(Task.FromResult("rendered"));
        var typed = FlowInputValues<string>.From(CreateInputBag("abc", rendering));

        //act
        var result = await typed.Render("tpl");

        //assert
        result.Should().Be("rendered");
    }

    [Fact]
    public void WhenFlowInputValuesSerializeString_ThenReturnsAsIs()
    {
        //arrange
        var typed = FlowInputValues<string>.From(CreateInputBag("abc"));

        //act
        var result = typed.Serialize("abc");

        //assert
        result.Should().Be("abc");
    }

    [Fact]
    public void WhenFlowInputValuesBagAccessed_ThenUntypedBagAndObjectAreExposed()
    {
        //arrange
        var typed = FlowInputValues<string>.From(CreateInputBag("abc"));

        //act
        var bag = typed.Bag;

        //assert
        bag.Should().NotBeNull();
        typed.InputAsObject.Should().Be("abc");
    }

    // --- Test helpers ---

    private static FlowGlobalInputBag CreateInputBag(object? input, IRenderingService? rendering = null)
    {
        var bag = new FlowGlobalInputBag(
            rendering ?? Substitute.For<IRenderingService>(),
            Substitute.For<ISerializer>(),
            null);
        var flow = Substitute.For<IFlow>();
        flow.Name.Returns("test-flow");
        flow.HaveInput.Returns(true);
        var flowContext = Substitute.For<IFlowContext>();
        flowContext.Flow.Returns(flow);
        flowContext.Input.Returns(input);
        bag.FromContext(flowContext);
        return bag;
    }

    // --- Test doubles ---

    private class TestNoInputNoOutputFlow : NoInputNoOutputFlow
    {
        public override string Name => "test-noio";
        public override string Definition => "test";
        protected override Task ApplyToPipelineImpl(IPipeline pipeline, IFlowGlobalInputBag inputs)
        {
            pipeline.AddBlock(new LogBlock());
            return Task.CompletedTask;
        }
    }

    private class TestNoInputFlow : NoInputFlow<string>
    {
        public bool WasCalled { get; private set; }

        public override string Name => "test-noinput";
        public override string Definition => "test";
        public override string? OutputSchema => null;
        public override string? BuildTypedOutput() => "output";
        protected override Task ApplyToPipelineImpl(IPipeline pipeline, IFlowGlobalInputBag inputs)
        {
            WasCalled = true;
            return Task.CompletedTask;
        }
    }

    private class TestNoOutputFlow : NoOutputFlow<TestNoOutputFlow.InputDto>
    {
        public InputDto? ReceivedInput { get; private set; }

        public override string Name => "test-nooutput";
        public override string Definition => "test";
        protected override Task ApplyToPipelineImpl(IPipeline pipeline, IFlowGlobalInputBag<InputDto> inputs)
        {
            ReceivedInput = inputs.Input;
            return Task.CompletedTask;
        }

        public class InputDto;
    }

    private class TestFlow : FlowBase<string, int>
    {
        public string? ReceivedInput { get; private set; }

        public override string Name => "test-flow";
        public override string Definition => "test";
        public override int BuildTypedOutput() => 42;
        protected override Task ApplyToPipelineImpl(IPipeline pipeline, IFlowGlobalInputBag<string> inputs)
        {
            ReceivedInput = inputs.Input;
            return Task.CompletedTask;
        }
    }
}
