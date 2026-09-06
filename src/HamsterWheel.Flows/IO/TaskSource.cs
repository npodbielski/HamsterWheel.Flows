using HamsterWheel.Flows.Blocks;

namespace HamsterWheel.Flows.IO;

public class TaskSource<T> : ITaskSource<T>
{
    public TaskSource()
    {
    }

    public TaskSource(T defaultValue) => Const = defaultValue;

    private Task<T>? _singleSource;
    private List<IAsyncEnumerable<T>> _multiSources = [];
    private bool _isConst;
    public bool IsSet => IsSingle || IsMulti;

    public bool IsSingle => _singleSource is not null || _isConst;

    public bool IsMulti => _multiSources.Count > 0;

    public void SetSource<T1>(IPipelineBlock<T1> sourceBlock) where T1 : class, T
    {
        if (sourceBlock.Result.IsSingle)
        {
            SetSource(ResultFromOtherBlock());
        }
        else
        {
            _multiSources.Add(sourceBlock.Result.AsEnumerable());
        }

        return;

        async Task<T> ResultFromOtherBlock() => await sourceBlock.Result.SingleValue;
    }

    public void SetSource(IAsyncEnumerable<T> source) => _multiSources.Add(source);

    public void SetSource(IPipelineBlock<T> sourceBlock)
    {
        if (sourceBlock.Result.IsSingle)
        {
            SetSource(sourceBlock.Result.SingleValue);
        }
        else
        {
            _multiSources.Add(sourceBlock.Result.AsEnumerable());
        }
    }

    public void SetSource<T1>(Task<T1> multiSource) where T1 : IEnumerable<T>
    {
        _multiSources.Add(ToAsyncEnumerable());
        return;

        async IAsyncEnumerable<T> ToAsyncEnumerable()
        {
            foreach (var item in await multiSource)
            {
                yield return item;
            }
        }
    }

    public void SetSource(Task<T> task)
    {
        _isConst = false;
        _singleSource = task;
        _multiSources = [];
    }

    public T Const
    {
        get => _isConst ? field : throw new ConstTaskSourceNotSetException();
        set
        {
            _isConst = true;
            _singleSource = null;
            _multiSources = [];
            field = value;
        }
    } = default!;

    public async IAsyncEnumerable<T> GetMulti()
    {
        if (!IsSet)
        {
            yield break;
        }

        if (IsMulti)
        {
            IAsyncEnumerable<T> combined;
            if (_multiSources.Count > 0)
            {
                combined = _multiSources.Skip(1)
                    .Aggregate(_multiSources[0], (current, source) => current.Concat(source));
            }
            else
            {
                combined = _multiSources[0];
            }

            await foreach (var item in combined)
            {
                yield return item;
            }

            yield break;
        }

        yield return await GetSingle();
    }

    public async Task<T> GetSingle()
    {
        if (!IsSet)
        {
            return default!;
        }

        return IsSingle ? _isConst ? Const : await _singleSource! : throw new CannotFetchTaskSourceAsSingleException();
    }
}
