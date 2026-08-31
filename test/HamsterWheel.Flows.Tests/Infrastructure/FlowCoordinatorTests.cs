using FluentAssertions;
using HamsterWheel.Flows;
using HamsterWheel.Flows.Auth;
using HamsterWheel.Flows.Pipelines;
using NSubstitute;

namespace HamsterWheel.Flows.Tests.Infrastructure;

public class FlowCoordinatorTests
{
    [Fact]
    public void WhenAttachPipeline_ThenPipelineIsAddedToActivePipelines()
    {
        //arrange
        var coordinator = new FlowCoordinator();
        var pipeline = Substitute.For<IPipeline>();
        var userService = Substitute.For<IFlowUserService>();

        //act
        coordinator.AttachPipeline(pipeline, userService);

        //assert
        coordinator.ActivePipelines.Should().ContainSingle().Which.Should().BeSameAs(pipeline);
        coordinator.FlowUserService.Should().BeSameAs(userService);
    }

    [Fact]
    public void WhenAuthorizeFlowWithAnonymousAllowed_ThenNoException()
    {
        //arrange
        var coordinator = new FlowCoordinator();
        var flow = Substitute.For<IFlow>();
        flow.DoesAllowAnonymousRuns.Returns(true);
        var userService = Substitute.For<IFlowUserService>();
        coordinator.AttachPipeline(Substitute.For<IPipeline>(), userService);

        //act
        var action = () => coordinator.Authorize(flow);

        //assert
        action.Should().NotThrow();
    }

    [Fact]
    public void WhenAuthorizeFlowWithoutAnonymousAndNoUser_ThenThrowsUserRequiredException()
    {
        //arrange
        var coordinator = new FlowCoordinator();
        var flow = Substitute.For<IFlow>();
        flow.DoesAllowAnonymousRuns.Returns(false);
        flow.Name.Returns("TestFlow");
        var userService = Substitute.For<IFlowUserService>();
        userService.Id.Returns("");
        coordinator.AttachPipeline(Substitute.For<IPipeline>(), userService);

        //act
        var action = () => coordinator.Authorize(flow);

        //assert
        action.Should().Throw<Exception>()
            .WithMessage("*user needs to be provided*");
    }

    [Fact]
    public void WhenAuthorizeFlowWithoutAnonymousAndWithUser_ThenNoException()
    {
        //arrange
        var coordinator = new FlowCoordinator();
        var flow = Substitute.For<IFlow>();
        flow.DoesAllowAnonymousRuns.Returns(false);
        var userService = Substitute.For<IFlowUserService>();
        userService.Id.Returns("user-123");
        coordinator.AttachPipeline(Substitute.For<IPipeline>(), userService);

        //act
        var action = () => coordinator.Authorize(flow);

        //assert
        action.Should().NotThrow();
    }

    [Fact]
    public void WhenSetSuccess_ThenDoesNotThrow()
    {
        //arrange
        var coordinator = new FlowCoordinator();

        //act
        var action = () => coordinator.SetSuccess(false);

        //assert
        action.Should().NotThrow();
    }
}
