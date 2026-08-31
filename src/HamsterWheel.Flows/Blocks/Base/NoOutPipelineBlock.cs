using HamsterWheel.Flows.IO;

namespace HamsterWheel.Flows.Blocks.Base;

public abstract class NoOutPipelineBlock<TInput, TInputSource> : BlockBase, IBlockInput<TInputSource>
    where TInput : class
    where TInputSource : class, IInputSourceEnumerator<TInput>, new()
{
    public override TInputSource Inputs { get; } = new();
    
    // ReSharper disable once MemberCanBeProtected.Global -> this is meant to be public API to wrap blocks on each other
    public abstract Task RunForInput(TInput input, CancellationToken token);

    protected override async Task RunImpl(CancellationToken token)
    {
        await foreach (var input in Inputs.Get(token))
        {
            await RunForInput(input, token);
        }

        SetResult();
    }
}