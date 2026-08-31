using System.Runtime.CompilerServices;
using HamsterWheel.Flows.IO;

namespace HamsterWheel.Flows.Blocks.Os.IO;

public class JoinPathsInputTaskSource : IComplexInputSource<JoinPathsInput>
{
    public TaskSource<string> First { get; } = new();
    public TaskSource<string> Second { get; } = new();

    public ITaskSource<JoinPathsInput> EntireInput { get; } = new TaskSource<JoinPathsInput>();
    public bool AllSingle => ((ITaskSource[]) [First, Second]).All(i => i.IsSingle || !i.IsSet);

    public async IAsyncEnumerable<JoinPathsInput>
        Get([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (First.IsSingle && Second.IsSingle)
        {
            yield return new JoinPathsInput(await First.GetSingle(), await Second.GetSingle());
            yield break;
        }

        var enumerator1 = First.GetMulti().GetAsyncEnumerator(cancellationToken);
        var enumerator2 = Second.GetMulti().GetAsyncEnumerator(cancellationToken);

        while ((await enumerator1.MoveNextAsync() || First.IsSingle) &&
               (await enumerator2.MoveNextAsync() || Second.IsSingle))
        {
            yield return new JoinPathsInput(
                (First.IsSingle ? await First.GetSingle() : enumerator1.Current)!,
                (Second.IsSingle ? await Second.GetSingle() : enumerator2.Current)!);
        }
    }
}