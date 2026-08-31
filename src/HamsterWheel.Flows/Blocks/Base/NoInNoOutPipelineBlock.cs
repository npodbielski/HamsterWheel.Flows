namespace HamsterWheel.Flows.Blocks.Base;

public abstract class NoInNoOutPipelineBlock(bool setResult = true) : BlockBase
{
    // ReSharper disable once MemberCanBeProtected.Global -> this is meant to be public API to wrap blocks on each other
    public abstract Task RunForInput();

    protected override async Task RunImpl(CancellationToken token)
    {
        await RunForInput();

        if (setResult)
        {
            SetResult();
        }
    }
}