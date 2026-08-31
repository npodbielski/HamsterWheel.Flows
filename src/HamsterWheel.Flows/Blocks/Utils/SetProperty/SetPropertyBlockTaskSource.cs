using System.Runtime.CompilerServices;
using HamsterWheel.Flows.IO;

namespace HamsterWheel.Flows.Blocks.Utils;

public class SetPropertyBlockTaskSource : IComplexInputSource<SetPropertyBlockInput>
{
    public TaskSource<string> PropertyPath { get; } = new();
    public TaskSource<object> Object { get; } = new();
    public TaskSource<object?> Value { get; } = new();
    public TaskSource<Map?> Map { get; } = new();

    public ITaskSource<SetPropertyBlockInput> EntireInput { get; } = new TaskSource<SetPropertyBlockInput>();

    public bool AllSingle => ((ITaskSource[]) [PropertyPath, Object, Value, Map]).All(i => i.IsSingle || !i.IsSet);

    public async IAsyncEnumerable<SetPropertyBlockInput> Get([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (PropertyPath.IsSingle && Object.IsSingle && Value.IsSingle && (Map.IsSingle || !Map.IsSet))
        {
            yield return new SetPropertyBlockInput
            {
                PropertyPath = await PropertyPath.GetSingle(),
                Object = await Object.GetSingle(),
                Value = await Value.GetSingle(),
                Map = await Map.GetSingle()
            };
            yield break;
        }

        var propertyPathCollection = PropertyPath.GetMulti().GetAsyncEnumerator(cancellationToken);
        var objectCollection = Object.GetMulti().GetAsyncEnumerator(cancellationToken);
        var valueCollection = Value.GetMulti().GetAsyncEnumerator(cancellationToken);
        var transformationCollection = Map.GetMulti().GetAsyncEnumerator(cancellationToken);

        while ((await propertyPathCollection.MoveNextAsync() || PropertyPath.IsSingle) &&
               (await objectCollection.MoveNextAsync() || Object.IsSingle) &&
               (await valueCollection.MoveNextAsync() || Value.IsSingle) &&
               (await transformationCollection.MoveNextAsync() || Map.IsSingle || !Map.IsSet))
        {
            yield return new SetPropertyBlockInput
            {
                PropertyPath = PropertyPath.IsSingle ? await PropertyPath.GetSingle() : propertyPathCollection.Current,
                Object = Object.IsSingle ? await Object.GetSingle() : objectCollection.Current,
                Value = Value.IsSingle ? await Value.GetSingle() : valueCollection.Current,
                Map = Map.IsSingle ? await Map.GetSingle() : transformationCollection.Current,
            };
        }
    }
}