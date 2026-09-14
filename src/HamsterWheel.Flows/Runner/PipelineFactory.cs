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
        var pipeline = CreatePipeline(options, _progressObserver, _runLogObservers);
        flowCoordinator.AttachPipeline(pipeline, currentUserService);
        return pipeline;
    }

    /// <summary>
    /// Creates the pipeline of the run. A host whose pipeline carries host behaviour — a database
    /// transaction to roll back when the run throws, for instance — overrides this and returns its own
    /// <see cref="Pipeline"/> subclass; building the options and attaching the coordinator stays here.
    /// </summary>
    /// <param name="options">Creation options of the run.</param>
    /// <param name="progressObserver">The host's progress observer, when it has one.</param>
    /// <param name="runLogObservers">Observers every log line of this run is handed to.</param>
    protected virtual IPipeline CreatePipeline(IPipelineCreationOptions options,
        IFlowProgressObserver? progressObserver,
        IReadOnlyList<IFlowRunLogObserver> runLogObservers) =>
        new Pipeline(options, flowCoordinator, blockFactory, pipelineLogger, progressObserver, runLogObservers);

    protected virtual IFlowUserService GetCurrentUserService(string? userId) =>
        new FixedUserService(userId, userPermissionsService.GetPermissions(userId));
}
