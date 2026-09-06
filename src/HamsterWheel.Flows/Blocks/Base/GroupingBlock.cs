using HamsterWheel.Flows.IO;

namespace HamsterWheel.Flows.Blocks.Base;

public abstract class GroupingBlock<TInput, TOutput, TInputSource> : BlockBase, IPipelineBlock<TOutput[]>,
    IBlockInput<TInputSource>
    where TInput : class
    where TInputSource : class, IInputSourceEnumerator<TInput>, new()
{
    private readonly BlockResult<TOutput[]> _result;

    protected GroupingBlock() => _result = new BlockResult<TOutput[]>(this);

    public IBlockResult<TOutput[]> Result => _result;
    public bool SingleOutput => Inputs.AllSingle;
    public override TInputSource Inputs { get; } = new();

    // ReSharper disable once MemberCanBeProtected.Global -> this is meant to be public API to wrap blocks on each other
    public abstract Task<IEnumerable<TOutput>> RunForInput(IEnumerable<TInput> input);

    protected override async Task RunImpl(CancellationToken token)
    {
        var input = Inputs.Get(token);
        var transformedInput = (IAsyncEnumerable<TInput>?)InputTransformer?.Invoke(Context.Pipeline.Logger, input);
        var result = await RunForInput((transformedInput ?? input).ToBlockingEnumerable(cancellationToken: token));
        PushOutput(result);
    }

    protected override void SetResult()
    {
        base.SetResult();
        Result.Finish();
    }

    private void PushOutput(IEnumerable<TOutput> result) => _result.PushOutput(result.ToArray());
}
