using System.Runtime.CompilerServices;
using HamsterWheel.Flows.IO;

namespace HamsterWheel.Flows.Blocks.Utils;

public class CreateObjectTaskSource : IComplexInputSource<CreateObjectInput>
{
    public TaskSource<string> Property1 { get; } = new();
    public TaskSource<string?> Property2 { get; } = new();
    public TaskSource<string?> Property3 { get; } = new();
    public TaskSource<object> Object1 { get; } = new();
    public TaskSource<object?> Object2 { get; } = new();
    public TaskSource<object?> Object3 { get; } = new();

    public ITaskSource<CreateObjectInput> EntireInput { get; } = new TaskSource<CreateObjectInput>();

    public bool AllSingle =>
        ((ITaskSource[]) [Property1, Property2, Property3, Object1, Object2, Object3]).All(i => i.IsSingle || !i.IsSet);

    public async IAsyncEnumerable<CreateObjectInput> Get(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (Object1.IsSingle &&
            (Object2.IsSingle || !Object2.IsSet) &&
            (Object3.IsSingle || !Object3.IsSet) &&
            Property1.IsSingle &&
            (Property2.IsSingle || !Property2.IsSet) &&
            (Property3.IsSingle || !Property3.IsSet)
           )
        {
            var input = new CreateObjectInput
            {
                Property1 = await Property1.GetSingle(),
                Property2 = await Property2.GetSingle(),
                Property3 = await Property3.GetSingle(),
                Object1 = await Object1.GetSingle(),
                Object2 = await Object2.GetSingle(),
                Object3 = await Object3.GetSingle()
            };
            yield return input;
            yield break;
        }

        var object1Enum = Object1.GetMulti().GetAsyncEnumerator(cancellationToken);
        var object2Enum = Object2.GetMulti().GetAsyncEnumerator(cancellationToken);
        var object3Enum = Object3.GetMulti().GetAsyncEnumerator(cancellationToken);
        var property1Enum = Property1.GetMulti().GetAsyncEnumerator(cancellationToken);
        var property2Enum = Property2.GetMulti().GetAsyncEnumerator(cancellationToken);
        var property3Enum = Property3.GetMulti().GetAsyncEnumerator(cancellationToken);

        while ((await object1Enum.MoveNextAsync() || Object1.IsSingle)
               && (await object2Enum.MoveNextAsync() || Object2.IsSingle || !Object2.IsSet)
               && (await object3Enum.MoveNextAsync() || Object3.IsSingle || !Object3.IsSet)
               && (await property1Enum.MoveNextAsync() || Property1.IsSingle)
               && (await property2Enum.MoveNextAsync() || Property2.IsSingle || !Property2.IsSet)
               && (await property3Enum.MoveNextAsync() || Property3.IsSingle || !Property3.IsSet)
              )
        {
            yield return new CreateObjectInput
            {
                Object1 = Object1.IsSingle ? await Object1.GetSingle() : object1Enum.Current,
                Object2 = Object2.IsSingle ? await Object2.GetSingle() : object2Enum.Current,
                Object3 = Object3.IsSingle ? await Object3.GetSingle() : object3Enum.Current,
                Property1 = (Property1.IsSingle ? await Property1.GetSingle() : property1Enum.Current)!,
                Property2 = Property2.IsSingle ? await Property2.GetSingle() : property2Enum.Current,
                Property3 = Property3.IsSingle ? await Property3.GetSingle() : property3Enum.Current
            };
        }
    }
}