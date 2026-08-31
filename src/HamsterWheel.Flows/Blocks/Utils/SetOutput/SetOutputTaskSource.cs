using System.Runtime.CompilerServices;
using HamsterWheel.Flows.IO;

namespace HamsterWheel.Flows.Blocks.Utils;

public class SetOutputTaskSource : IComplexInputSource<SetOutputInput>
{
    public TaskSource<string?> PropertyPath { get; } = new();
    public TaskSource<object?> Value { get; } = new();
    public TaskSource<Map?> Map { get; } = new();

    public ITaskSource<SetOutputInput> EntireInput { get; } = new TaskSource<SetOutputInput>();

    public bool AllSingle => ((ITaskSource[])[PropertyPath, Value, Map]).All(i => i.IsSingle || !i.IsSet);

    public async IAsyncEnumerable<SetOutputInput> Get(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if ((PropertyPath.IsSingle || !PropertyPath.IsSet) && Value.IsSingle && (Map.IsSingle || !Map.IsSet))
        {
            yield return new SetOutputInput
            {
                PropertyPath = await PropertyPath.GetSingle(),
                Value = await Value.GetSingle(),
                Map = await Map.GetSingle()
            };
            yield break;
        }

        var propertyPathCollection = PropertyPath.GetMulti().GetAsyncEnumerator(cancellationToken);
        var valueCollection = Value.GetMulti().GetAsyncEnumerator(cancellationToken);
        var transformationCollection = Map.GetMulti().GetAsyncEnumerator(cancellationToken);

        while ((await propertyPathCollection.MoveNextAsync() || PropertyPath.IsSingle || !PropertyPath.IsSet) &&
               (await valueCollection.MoveNextAsync() || Value.IsSingle) &&
               (await transformationCollection.MoveNextAsync() || Map.IsSingle || !Map.IsSet))
        {
            yield return new SetOutputInput
            {
                PropertyPath = PropertyPath.IsSingle ? await PropertyPath.GetSingle() : propertyPathCollection.Current,
                Value = Value.IsSingle ? await Value.GetSingle() : valueCollection.Current,
                Map = Map.IsSingle ? await Map.GetSingle() : transformationCollection.Current,
            };
        }
    }
}