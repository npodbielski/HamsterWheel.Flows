using HamsterWheel.Flows.IO;

namespace HamsterWheel.Flows.Blocks.Base;

public abstract class PipelineBlock<TInput, TOutput, TInputSource> : BlockBase, IPipelineBlock<TOutput>,
    IBlockInput<TInputSource>
    where TInput : class
    where TInputSource : class, IInputSourceEnumerator<TInput>, new()
{
    private readonly BlockResult<TOutput> _result;

    protected PipelineBlock() => _result = new BlockResult<TOutput>(this);

    public IBlockResult<TOutput> Result => _result;
    public virtual bool SingleOutput => Inputs.AllSingle;
    public override TInputSource Inputs { get; } = new();

    protected override async Task RunImpl(CancellationToken token)
    {
        await foreach (var input in Inputs.Get(token))
        {
            var output = await RunForInput(input, token);
            PushOutput(output);
        }

        SetResult();
    }

    public abstract Task<TOutput> RunForInput(TInput input, CancellationToken token);

    protected override void SetResult()
    {
        base.SetResult();
        Result.Finish();
    }

    private void PushOutput(TOutput output) => _result.PushOutput(output);
}