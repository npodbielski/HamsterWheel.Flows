using HamsterWheel.Flows.IO;
using HamsterWheel.Flows.Monitoring;
using HamsterWheel.Flows.Pipelines;
using HamsterWheel.Utils;

namespace HamsterWheel.Flows.Blocks.Base;

public abstract class BlockBase : IPipelineBlock
{
    private IFlowContext _parentContext = null!;
    private readonly string _blockTypeName;
    private readonly List<Task> _triggers = [];
    private readonly TaskCompletionSource _tcs = new();

    protected BlockBase() => _blockTypeName = GetType().Name;

    public Task<bool>? Condition { get; set; }

    public Task Completion => _tcs.Task;

    protected IFlowContext Context { get; set; } = null!;
    public abstract IInputInfo Inputs { get; }
    public string Id { get; init; } = null!;
    public string? Description { get; init; }

    public Func<IPipelineLogger, object?, object?>? InputTransformer { get; set; }

    public async Task Run(CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            return;
        }

        await Context.Pipeline.Initialized.WaitAsync(token);

        if (token.IsCancellationRequested)
        {
            return;
        }

        try
        {
            await Task.WhenAll(_triggers).WaitAsync(token);
        }
        catch (Exception e)
        {
            SetException(new BlockTriggerException(Id, e));
            throw;
        }

        if (token.IsCancellationRequested)
        {
            return;
        }

        if (Condition != null)
        {
            try
            {
                if (!await Condition.WaitAsync(token))
                {
                    SetResult();
                    return;
                }
            }
            catch (Exception e)
            {
                SetException(new BlockConditionException(Id, e));
                throw;
            }
        }

        try
        {
            await RunImpl(token);
        }
        catch (Exception e)
        {
            SetException(new BlockOperationException(Id, e));
            throw;
        }
    }

    public void Init(IFlowContext context)
    {
        _parentContext = context;
        Context = ((FlowContext)_parentContext).GetChildContext();
        PostInit();
    }

    public void TriggerAfter(Task task) => _triggers.Add(task);

    public void TriggerAfter(params IPipelineBlock[] blocks)
    {
        foreach (var block in blocks)
        {
            TriggerAfter(block.Completion);
        }
    }

    protected virtual void PostInit()
    {
    }

    protected abstract Task RunImpl(CancellationToken token);

    protected virtual void SetException(Exception exception) => _tcs.TrySetException(exception);
    protected virtual void SetResult() => _tcs.TrySetResult();

    protected void Log(string message, FlowLogLevel level = FlowLogLevel.Info) =>
        Context.Pipeline.Logger.Log(new FlowLogMessage(level, DateTimeOffset.UtcNow, message, Id, _blockTypeName));

    protected void LogIfNotEmpty(string? message)
    {
        if (!message.IsNullOrWhiteSpace())
        {
            Log(message);
        }
    }
}