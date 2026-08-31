using HamsterWheel.Flows.Blocks;

namespace HamsterWheel.Flows.IO;

public class BlockResult<TOutput>(IPipelineBlock<TOutput> owner) : IBlockResult<TOutput>
{
    private TOutput? _lastOutput;
    private readonly TaskCompletionSource<TOutput> _tcs = new();
    private readonly List<LinkedBlockEnumerator<TOutput>> _linkedEnumerators = [];
    public Task<TOutput> SingleValue => _tcs.Task;
    public bool IsSingle => owner.SingleOutput;

    public IAsyncEnumerable<TOutput> AsEnumerable()
    {
        var enumerator = new LinkedBlockEnumerator<TOutput>(owner);
        _linkedEnumerators.Add(enumerator);
        return enumerator.Get();
    }

    public void PushOutput(TOutput output)
    {
        _lastOutput = output;
        if (IsSingle)
        {
            _tcs.TrySetResult(output);
        }
        else
        {
            foreach (var enumerator in _linkedEnumerators)
            {
                enumerator.Push(output);
            }
        }
    }

    public void Finish()
    {
        if (IsSingle)
        {
            return;
        }

        foreach (var enumerator in _linkedEnumerators)
        {
            enumerator.Finish();
        }
    }

    public TOutput GetLastOutput() => _lastOutput ?? throw new InvalidOperationException("Block has no output");
}