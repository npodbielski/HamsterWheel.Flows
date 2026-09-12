using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Monitoring;
using HamsterWheel.Flows.Runner;

namespace HamsterWheel.Flows.Pipelines;

public class Pipeline(
    IPipelineCreationOptions creationOptions,
    IFlowCoordinator flowCoordinator,
    IBlockFactory blockFactory,
    IPipelineLogger pipelineLogger,
    IFlowProgressObserver? observer = null,
    IReadOnlyList<IFlowRunLogObserver>? runLogObservers = null) : IPipeline
{
    private readonly TaskCompletionSource _pipelineInitializationTask = new();
    private readonly IReadOnlyList<IFlowRunLogObserver>? runLogObservers = runLogObservers;
    private IPipelineLogger logger = pipelineLogger;
    public IPipelineCreationOptions Options { get; } = creationOptions;
    public IFlowCoordinator Coordinator { get; set; } = flowCoordinator;
    public Task Initialized => _pipelineInitializationTask.Task;

    /// <summary>
    /// The logger of this run: the host's shared logger, wrapped (once the flow is attached) so that
    /// the lines written through it also reach this run's <see cref="IFlowRunLogObserver"/>s with
    /// this run's identity — which the shared logger cannot supply, being shared by every pipeline.
    /// Unwrapped when nobody observes run logs.
    /// </summary>
    public IPipelineLogger Logger => logger;
    public IFlow Flow { get; private set; } = null!;
    public IFlowContext FlowContext { get; private set; } = null!;
    protected List<IPipelineBlock> Blocks { get; } = [];

    public void AttachCoordinator(IFlowCoordinator coordinator) => Coordinator = coordinator;

    public void AttachFlowContext(IFlowContext flowContext)
    {
        FlowContext = flowContext;
        Flow = flowContext.Flow;

        if (runLogObservers is { Count: > 0 } observers)
        {
            //from here on the run has a name, so its log lines can be attributed to it
            logger = new RunLogPipelineLogger(logger, GetCurrentRun(), observers);
        }
    }

    public async Task<object?> Run(CancellationToken token = default)
    {
        await HandlePrePipelineInit();
        Init();
        await FinishPipelineInit();
        var run = GetCurrentRun();
        NotifyFlowStarted(run);
        var estimatedMaxRunTime = (Flow.AverageTime is not null ? Flow.AverageTime * 3 : TimeSpan.FromMinutes(5)).Value;
        var delay = Task.Delay(estimatedMaxRunTime, token);
        var blockRuns = Blocks.Select(b => (Block: b, Task: b.Run(token))).ToArray();
        foreach (var (block, runTask) in blockRuns)
        {
            //execute synchronously so block notifications are guaranteed to precede the flow-level ones
            runTask.ContinueWith(t => NotifyBlockFinished(run, block, t), CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        var tasks = Task.WhenAll(blockRuns.Select(r => r.Task)).WaitAsync(token);
        var task = await Task.WhenAny(tasks, delay).WaitAsync(token);
        if (task == delay)
        {
            var error = new FlowCancelledException(Blocks.Where(b => !b.Completion.IsCompleted));
            NotifyFlowFailed(run, error);
            throw error;
        }

        if (task.IsFaulted)
        {
            await HandleException(token);

            var flowExceptions = task.Exception.InnerExceptions.Where(e => e is not BlockException).ToArray();
            flowExceptions = [.. flowExceptions.DistinctBy(e => e.Message)];
            if (flowExceptions.Length > 1)
            {
                var error = new AggregateException(flowExceptions);
                NotifyFlowFailed(run, error);
                throw error;
            }

            var exception = flowExceptions.FirstOrDefault();
            if (exception is not null)
            {
                var error = new AggregateException(exception);
                NotifyFlowFailed(run, error);
                throw error;
            }

            NotifyFlowFailed(run, task.Exception);
            throw task.Exception;
        }

        if (FlowContext is ISuccessHandler successHandler && task.IsCompletedSuccessfully)
        {
            successHandler.SetSuccess(Options.IsSubFlow);
        }

        NotifyFlowCompleted(run, FlowContext.Output);
        return FlowContext.Output;
    }

    public void AddBlock(IPipelineBlock block)
    {
        if (!Blocks.Contains(block))
        {
            Blocks.Add(block);
        }
    }

    public T AddBlock<T>(string? name = null, string? description = null) where T : class, IPipelineBlock, new()
    {
        var block = blockFactory.Create<T>(name, description);
        AddBlock(block);
        return block;
    }

    internal async Task FinishPipelineInit()
    {
        _pipelineInitializationTask.TrySetResult();

        await HandlePipelineInit();

        Logger.Log($"Flow: '{Flow.Name}' version: {Flow.Version}");
    }

    protected virtual Task HandlePipelineInit() => Task.CompletedTask;
    protected virtual Task HandlePrePipelineInit() => Task.CompletedTask;
    protected virtual Task HandleException(CancellationToken token) => Task.CompletedTask;

    private void NotifyFlowStarted(FlowRunIdentity run) =>
        SafeNotify(() => observer?.FlowStarted(run, Blocks.Count));

    private void NotifyBlockFinished(FlowRunIdentity run, IPipelineBlock block, Task task)
    {
        if (task.IsFaulted)
        {
            //the typed block exception (BlockOperationException/BlockTriggerException/BlockConditionException)
            //is faulted onto the block's Completion task, the run task may carry the raw exception
            var exception = block.Completion.IsFaulted
                ? block.Completion.Exception!.Flatten().InnerExceptions.First()
                : task.Exception!.Flatten().InnerExceptions.First();
            SafeNotify(() => observer?.BlockFailed(run, block, exception));
        }
        else
        {
            SafeNotify(() => observer?.BlockCompleted(run, block));
        }
    }

    private void NotifyFlowCompleted(FlowRunIdentity run, object? output) =>
        SafeNotify(() => observer?.FlowCompleted(run, output));

    private void NotifyFlowFailed(FlowRunIdentity run, Exception error) =>
        SafeNotify(() => observer?.FlowFailed(run, error));

    //a faulting observer must never break a flow
    private void SafeNotify(Action notify)
    {
        try
        {
            notify();
        }
        catch (Exception e)
        {
            Logger.Log(new FlowLogMessage(FlowLogLevel.Error, DateTimeOffset.UtcNow,
                $"Flow progress observer failed: {e.Message}", null, nameof(IFlowProgressObserver)));
        }
    }

    //the run of this pipeline - what both its observers are told the lines and steps belong to
    private FlowRunIdentity GetCurrentRun() => new(Options.RunId, new FlowName(Flow.Name), Options.UserId)
    {
        IsSubFlow = Options.IsSubFlow
    };

    private void Init() => Blocks.ForEach(b => b.Init(FlowContext));
}