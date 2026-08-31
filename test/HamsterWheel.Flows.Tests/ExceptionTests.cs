using FluentAssertions;
using HamsterWheel.Flows;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Blocks.Os;
using HamsterWheel.Flows.Blocks.Utils;
using HamsterWheel.Flows.Runner;

namespace HamsterWheel.Flows.Tests;

public class ExceptionTests
{
    [Fact]
    public void WhenCouldNotCreateTypeExceptionConstructed_ThenMessageContainsBlockAndType()
    {
        //arrange
        // (none needed)

        //act
        var exception = new CouldNotCreateTypeException("block-1", typeof(string));

        //assert
        exception.Message.Should().Contain("block-1");
        exception.Message.Should().Contain(nameof(CreateObjectBlock));
        exception.Message.Should().Contain("String");
    }

    [Fact]
    public void WhenIncorrectMapOperationValueExceptionConstructed_ThenMessageContainsOperation()
    {
        //arrange
        // (none needed)

        //act
        var exception = new IncorrectMapOperationValueException(MapOperation.HLinq);

        //assert
        exception.Message.Should().Contain(nameof(MapOperation.HLinq));
    }

    [Fact]
    public void WhenSetOutputBlockUsedOnNullOutputExceptionConstructed_ThenMessageContainsBlockName()
    {
        //arrange
        // (none needed)

        //act
        var exception = new SetOutputBlockUsedOnNullOutputException();

        //assert
        exception.Message.Should().Contain(nameof(SetOutputBlock));
    }

    [Fact]
    public void WhenInvalidOsCommandExceptionConstructed_ThenMessageContainsTypeAndInput()
    {
        //arrange
        // (none needed)

        //act
        var exception = new InvalidOsCommandException<string>("bad command");

        //assert
        exception.Message.Should().Contain(typeof(string).FullName);
        exception.Message.Should().Contain("bad command");
    }

    [Fact]
    public void WhenOsCommandExceptionConstructed_ThenMessageContainsCommand()
    {
        //arrange
        // (none needed)

        //act
        var exception = new OsCommandException("ls -la");

        //assert
        exception.Message.Should().Be("Could not start command: ls -la");
    }

    [Fact]
    public void WhenBlockExceptionConstructedViaSubclass_ThenMessageAndInnerExceptionAreSet()
    {
        //arrange
        var inner = new InvalidOperationException("inner failure");

        //act
        var exception = new BlockTriggerException("block-2", inner);

        //assert
        exception.Message.Should().Contain("block-2");
        exception.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void WhenFlowExceptionConstructedViaSubclass_ThenMessageContainsFlowName()
    {
        //arrange
        // (none needed)

        //act
        var exception = new FlowNotFoundException(new FlowName("missing-flow"));

        //assert
        exception.Message.Should().Contain("missing-flow");
    }

    [Fact]
    public void WhenFlowCreationOptionsFlowNameInvalidExceptionConstructed_ThenMessageContainsFlowName()
    {
        //arrange
        // (none needed)

        //act
        var exception = new FlowCreationOptionsFlowNameInvalidException("bad-name");

        //assert
        exception.Message.Should().Contain("bad-name");
    }

    [Fact]
    public void WhenAmbiguousFlowNameExceptionConstructed_ThenMessageContainsNameAndExtensions()
    {
        //arrange
        // (none needed)

        //act
        var exception = new AmbiguousFlowNameException("my-flow", ["ext-1", "ext-2"]);

        //assert
        exception.Message.Should().Contain("my-flow");
        exception.Message.Should().Contain("ext-1");
        exception.Message.Should().Contain("ext-2");
    }

    [Fact]
    public void WhenMismatchedFlowOutputTypeExceptionWithActualType_ThenMessageContainsBothTypes()
    {
        //arrange
        // (none needed)

        //act
        var exception = new MismatchedFlowOutputTypeException<int>("my-flow", typeof(string));

        //assert
        exception.Message.Should().Contain("Int32");
        exception.Message.Should().Contain("String");
    }

    [Fact]
    public void WhenMismatchedFlowOutputTypeExceptionWithoutActualType_ThenMessageSaysMissing()
    {
        //arrange
        // (none needed)

        //act
        var exception = new MismatchedFlowOutputTypeException<int>("my-flow", null);

        //assert
        exception.Message.Should().Contain("Int32");
        exception.Message.Should().EndWith("missing.");
    }

    [Fact]
    public void WhenFlowNoInputExceptionConstructed_ThenMessageContainsFlowName()
    {
        //arrange
        // (none needed)

        //act
        var exception = new FlowNoInputException("my-flow");

        //assert
        exception.Message.Should().Contain("my-flow");
    }

    [Fact]
    public void WhenMismatchedFlowInputTypeExceptionWithType_ThenMessageContainsType()
    {
        //arrange
        // (none needed)

        //act
        var exception = new MismatchedFlowInputTypeException("my-flow", typeof(int));

        //assert
        exception.Message.Should().Contain("Int32");
    }

    [Fact]
    public void WhenMismatchedFlowInputTypeExceptionWithoutType_ThenMessageSaysMissing()
    {
        //arrange
        // (none needed)

        //act
        var exception = new MismatchedFlowInputTypeException("my-flow", null);

        //assert
        exception.Message.Should().Contain("missing");
    }

    [Fact]
    public void WhenDuringFlowAttachMissingUserExceptionConstructed_ThenMessageMentionsUserService()
    {
        //arrange
        // (none needed)

        //act
        var exception = new DuringFlowAttachMissingUserException();

        //assert
        exception.Message.Should().Contain(nameof(HamsterWheel.Flows.Auth.IFlowUserService));
    }

    [Fact]
    public void WhenInsufficientPermissionsToCreateBlockExceptionConstructed_ThenMessageContainsUserAndBlock()
    {
        //arrange
        var userId = Guid.NewGuid();

        //act
        var exception = new InsufficientPermissionsToCreateBlockException<OsCommandBlock>(userId);

        //assert
        exception.Message.Should().Contain(userId.ToString());
        exception.Message.Should().Contain(nameof(OsCommandBlock));
    }

    [Fact]
    public void WhenFlowCancelledExceptionConstructed_ThenMessageListsBlocks()
    {
        //arrange
        // (none needed)

        //act
        var exception = new FlowCancelledException([new OsCommandBlock { Id = "block-1" }]);

        //assert
        exception.Message.Should().Contain("block-1");
    }

    [Fact]
    public void WhenConstTaskSourceNotSetExceptionConstructed_ThenMessageExplainsCause()
    {
        //arrange
        // (none needed)

        //act
        var exception = new ConstTaskSourceNotSetException();

        //assert
        exception.Message.Should().Contain("constant");
    }

    [Fact]
    public void WhenCannotFetchTaskSourceAsSingleExceptionConstructed_ThenMessageExplainsCause()
    {
        //arrange
        // (none needed)

        //act
        var exception = new CannotFetchTaskSourceAsSingleException();

        //assert
        exception.Message.Should().Contain("single");
    }
}
