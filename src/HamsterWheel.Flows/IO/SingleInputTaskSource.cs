using System.Runtime.CompilerServices;

namespace HamsterWheel.Flows.IO;

public class SingleInputTaskSource<T> : TaskSource<T>, IInputSourceEnumerator<T>, ISingleInputSource
{
    public virtual bool AllSingle => IsSingle;

    public async IAsyncEnumerable<T> Get([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (IsSingle)
        {
            yield return await GetSingle();
            yield break;
        }

        await foreach (var item in GetMulti().WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }
}