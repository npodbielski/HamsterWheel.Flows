using System.Runtime.CompilerServices;
using HamsterWheel.Flows.IO;

namespace HamsterWheel.Flows.Blocks.Utils;

public class MapInputTaskSource : IComplexInputSource<MapInput>
{
    public TaskSource<MapOperation> Operation { get; } = new(MapOperation.Property);
    public TaskSource<string> Map { get; } = new();
    public TaskSource<object> Object { get; } = new();

    public ITaskSource<MapInput> EntireInput { get; } = new TaskSource<MapInput>();

    public bool AllSingle =>
        ((ITaskSource[]) [Operation, Map, Object]).All(i => i.IsSingle || !i.IsSet);

    public async IAsyncEnumerable<MapInput> Get([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (Operation.IsSingle && Map.IsSingle && Object.IsSingle)
        {
            yield return new MapInput(await Operation.GetSingle(), await Map.GetSingle(), await Object.GetSingle());
            yield break;
        }

        var enumerator1 = Operation.GetMulti().GetAsyncEnumerator(cancellationToken);
        var enumerator2 = Map.GetMulti().GetAsyncEnumerator(cancellationToken);
        var enumerator3 = Object.GetMulti().GetAsyncEnumerator(cancellationToken);

        while ((await enumerator1.MoveNextAsync() || Operation.IsSingle) &&
               (await enumerator2.MoveNextAsync() || Map.IsSingle) &&
               (await enumerator3.MoveNextAsync() || Object.IsSingle))
        {
            yield return new MapInput(
                Operation.IsSingle ? await Operation.GetSingle() : enumerator1.Current,
                (Map.IsSingle ? await Map.GetSingle() : enumerator2.Current)!,
                (Object.IsSingle ? await Object.GetSingle() : enumerator3.Current)!
            );
        }
    }
}