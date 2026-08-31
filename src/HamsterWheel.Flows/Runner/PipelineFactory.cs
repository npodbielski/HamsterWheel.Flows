using HamsterWheel.Flows.Auth;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Monitoring;
using HamsterWheel.Flows.Pipelines;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.Flows.Runner;

public class PipelineFactory(
    IFlowCoordinator flowCoordinator,
    IBlockFactory blockFactory,
    IUserPermissionsService userPermissionsService,
    IPipelineLogger pipelineLogger,
    IServiceProvider? services = null) : IPipelineFactory
{
    public virtual IPipeline Create(Action<IPipelineCreationOptions> configureOptions)
    {
        var options = new PipelineCreationOptions();
        configureOptions(options);
        var currentUserService = GetCurrentUserService(options.UserId);
        //the progress observer is opt-in - hosts that do not register one see no behavior change
        var observer = services?.GetService<IFlowProgressObserver>();
        var pipeline = new Pipeline(options, flowCoordinator, blockFactory, pipelineLogger, observer);
        flowCoordinator.AttachPipeline(pipeline, currentUserService);
        return pipeline;
    }

    protected virtual IFlowUserService GetCurrentUserService(string? userId) =>
        new FixedUserService(userId, userPermissionsService.GetPermissions(userId));
}