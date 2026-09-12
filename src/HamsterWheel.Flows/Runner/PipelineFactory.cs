using HamsterWheel.Flows.Auth;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Monitoring;
using HamsterWheel.Flows.Pipelines;

namespace HamsterWheel.Flows.Runner;

public class PipelineFactory(
    IFlowCoordinator flowCoordinator,
    IBlockFactory blockFactory,
    IUserPermissionsService userPermissionsService,
    IPipelineLogger pipelineLogger,
    IEnumerable<IFlowProgressObserver>? progressObservers = null,
    IEnumerable<IFlowRunLogObserver>? runLogObservers = null) : IPipelineFactory
{
    //observers are dependencies like any other: DI hands an empty collection where nobody registered
    //one, so hosts that do not observe runs see no behavior change. A progress observer is a single
    //notification sink, so the last registration wins, as it did when it was resolved.
    private readonly IFlowProgressObserver? _progressObserver = progressObservers?.LastOrDefault();
    private readonly IFlowRunLogObserver[] _runLogObservers = runLogObservers?.ToArray() ?? [];

    public virtual IPipeline Create(Action<IPipelineCreationOptions> configureOptions)
    {
        var options = new PipelineCreationOptions();
        configureOptions(options);
        var currentUserService = GetCurrentUserService(options.UserId);
        var pipeline = new Pipeline(options, flowCoordinator, blockFactory, pipelineLogger,
            _progressObserver, _runLogObservers);
        flowCoordinator.AttachPipeline(pipeline, currentUserService);
        return pipeline;
    }

    protected virtual IFlowUserService GetCurrentUserService(string? userId) =>
        new FixedUserService(userId, userPermissionsService.GetPermissions(userId));
}
