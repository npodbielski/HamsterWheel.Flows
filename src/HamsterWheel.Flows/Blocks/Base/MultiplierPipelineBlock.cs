using HamsterWheel.Flows.IO;

namespace HamsterWheel.Flows.Blocks.Base;

public abstract class MultiplierPipelineBlock<TInput, TOutput, TInputSource> : BlockBase, IPipelineBlock<TOutput>,
    IBlockInput<TInputSource>
    where TInput : class
    where TInputSource : class, IInputSourceEnumerator<TInput>, new()
{
    private readonly BlockResult<TOutput> _result;
    public IBlockResult<TOutput> Result => _result;

    /// <summary>
    /// Multiplier block never produces single output
    /// </summary>
    public bool SingleOutput => false;

    public override TInputSource Inputs { get; } = new();

    protected MultiplierPipelineBlock() => _result = new BlockResult<TOutput>(this);

    // ReSharper disable once MemberCanBeProtected.Global -> this is meant to be public API to wrap blocks on each other
    public abstract Task RunForInput(TInput input, Action<TOutput> pushOutput, CancellationToken token);

    protected override async Task RunImpl(CancellationToken token)
    {
        await foreach (var input in Inputs.Get(token))
        {
            var transformedInput = (TInput?)InputTransformer?.Invoke(Context.Pipeline.Logger, input);
            await RunForInput(transformedInput ?? input, PushOutput, token);
        }

        SetResult();
    }

    protected override void SetResult()
    {
        base.SetResult();
        Result.Finish();
    }

    private void PushOutput(TOutput i) => _result.PushOutput(i);
}