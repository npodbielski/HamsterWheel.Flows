using FluentAssertions;
using HamsterWheel.Flows;
using HamsterWheel.Flows.Auth;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Monitoring;
using HamsterWheel.Flows.Pipelines;
using HamsterWheel.Flows.Runner;
using NSubstitute;

namespace HamsterWheel.Flows.Tests.Infrastructure;

public class PipelineFactoryTests
{
    [Fact]
    public void WhenCreate_ThenPipelineIsWiredToCoordinator()
    {
        //arrange
        var coordinator = new FlowCoordinator();
        var blockFactory = Substitute.For<IBlockFactory>();
        var userPermissions = Substitute.For<IUserPermissionsService>();
        var logger = Substitute.For<IPipelineLogger>();
        var factory = new PipelineFactory(coordinator, blockFactory, userPermissions, logger);

        //act
        var pipeline = factory.Create(c => c.RunId = Guid.NewGuid());

        //assert
        pipeline.Should().NotBeNull();
        coordinator.ActivePipelines.Should().ContainSingle().Which.Should().BeSameAs(pipeline);
    }

    [Fact]
    public void WhenCreateWithOptions_ThenOptionsAreSet()
    {
        //arrange
        var coordinator = new FlowCoordinator();
        var blockFactory = Substitute.For<IBlockFactory>();
        var userPermissions = Substitute.For<IUserPermissionsService>();
        var logger = Substitute.For<IPipelineLogger>();
        var factory = new PipelineFactory(coordinator, blockFactory, userPermissions, logger);
        var runId = Guid.NewGuid();

        //act
        var pipeline = factory.Create(c =>
        {
            c.RunId = runId;
            c.UserId = "user-1";
            c.IsSubFlow = true;
        });

        //assert
        pipeline.Options.RunId.Should().Be(runId);
        pipeline.Options.UserId.Should().Be("user-1");
        pipeline.Options.IsSubFlow.Should().BeTrue();
    }

    [Fact]
    public void WhenCreate_ThenFlowUserServiceIsSetOnCoordinator()
    {
        //arrange
        var coordinator = new FlowCoordinator();
        var blockFactory = Substitute.For<IBlockFactory>();
        var userPermissions = Substitute.For<IUserPermissionsService>();
        userPermissions.GetPermissions("user-1").Returns(["perm1"]);
        var logger = Substitute.For<IPipelineLogger>();
        var factory = new PipelineFactory(coordinator, blockFactory, userPermissions, logger);

        //act
        factory.Create(c => c.UserId = "user-1");

        //assert
        coordinator.FlowUserService.Id.Should().Be("user-1");
        coordinator.FlowUserService.Permissions.Should().Contain("perm1");
    }

    [Fact]
    public void WhenCreatedWithRunLogObservers_ThenThePipelineHandsThemItsLines()
    {
        //arrange
        var coordinator = new FlowCoordinator();
        var blockFactory = Substitute.For<IBlockFactory>();
        var userPermissions = Substitute.For<IUserPermissionsService>();
        var logger = Substitute.For<IPipelineLogger>();
        var observer = Substitute.For<IFlowRunLogObserver>();
        var factory = new PipelineFactory(coordinator, blockFactory, userPermissions, logger, [], [observer]);
        var runId = Guid.NewGuid();
        var pipeline = factory.Create(c => c.RunId = runId);
        var flow = Substitute.For<IFlow>();
        flow.Name.Returns("test-flow");
        var flowContext = Substitute.For<IFlowContext>();
        flowContext.Flow.Returns(flow);
        pipeline.AttachFlowContext(flowContext);

        //act
        pipeline.Logger.Log("a line of this run");

        //assert
        observer.Received(1).Log(Arg.Is<FlowRunIdentity>(run => run.RunId == runId && run.FlowName.Name == "test-flow"),
            Arg.Is<IFlowLogMessage>(l => l.Message == "a line of this run"));
        logger.Received(1).Log(Arg.Is<IFlowLogMessage>(l => l.Message == "a line of this run"));
    }

    [Fact]
    public void WhenCreatedWithoutObservers_ThenThePipelineLogsStraightToTheHostLogger()
    {
        //arrange
        var coordinator = new FlowCoordinator();
        var blockFactory = Substitute.For<IBlockFactory>();
        var userPermissions = Substitute.For<IUserPermissionsService>();
        var logger = Substitute.For<IPipelineLogger>();
        var factory = new PipelineFactory(coordinator, blockFactory, userPermissions, logger);
        var pipeline = factory.Create(c => c.RunId = Guid.NewGuid());
        var flow = Substitute.For<IFlow>();
        var flowContext = Substitute.For<IFlowContext>();
        flowContext.Flow.Returns(flow);
        pipeline.AttachFlowContext(flowContext);

        //act
        pipeline.Logger.Log("a line");

        //assert
        logger.Received(1).Log("a line");
        pipeline.Logger.Should().BeSameAs(logger);
    }
}
