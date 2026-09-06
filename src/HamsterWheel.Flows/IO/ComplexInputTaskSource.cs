using System.Runtime.CompilerServices;

namespace HamsterWheel.Flows.IO;

public abstract class ComplexInputTaskSource<T> : IComplexInputSource<T>
{
    public abstract bool AllSingle { get; }
    public ITaskSource<T> EntireInput { get; } = new TaskSource<T>();

    public async IAsyncEnumerable<T> Get([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        switch (EntireInput)
        {
            case { IsSet: true, IsSingle: true }:
                yield return await EntireInput.GetSingle();
                yield break;
            case { IsSet: true, IsMulti: true }:
            {
                await foreach (var input in EntireInput.GetMulti().WithCancellation(cancellationToken))
                {
                    yield return input;
                }

                break;
            }
        }

        await foreach (var input in GetImpl(cancellationToken))
        {
            yield return input;
        }
    }

    protected abstract IAsyncEnumerable<T> GetImpl(CancellationToken cancellationToken = default);
}
