using System.Runtime.CompilerServices;
using HamsterWheel.Flows.IO;

namespace HamsterWheel.Flows.Blocks.Os.IO;

public class SaveFileInputTaskSource : ComplexInputTaskSource<SaveFileInput>
{
    public TaskSource<string> FileName { get; } = new();
    public TaskSource<string> Contents { get; } = new();
    public TaskSource<string> Directory { get; } = new();
    public override bool AllSingle => ((ITaskSource[]) [FileName, Contents, Directory]).All(i => i.IsSingle);

    protected override async IAsyncEnumerable<SaveFileInput> GetImpl(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (FileName.IsSingle && Contents.IsSingle && Directory.IsSingle)
        {
            yield return new SaveFileInput(
                await FileName.GetSingle(),
                await Contents.GetSingle(),
                await Directory.GetSingle());
            yield break;
        }

        var enumerator1 = FileName.GetMulti().GetAsyncEnumerator(cancellationToken);
        var enumerator2 = Contents.GetMulti().GetAsyncEnumerator(cancellationToken);
        var enumerator3 = Directory.GetMulti().GetAsyncEnumerator(cancellationToken);

        while ((await enumerator1.MoveNextAsync() || FileName.IsSingle) &&
               (await enumerator2.MoveNextAsync() || Contents.IsSingle) &&
               (await enumerator3.MoveNextAsync() || Directory.IsSingle))
        {
            yield return new SaveFileInput(
                FileName.IsSingle ? await FileName.GetSingle() : enumerator1.Current,
                Contents.IsSingle ? await Contents.GetSingle() : enumerator2.Current,
                Directory.IsSingle ? await Directory.GetSingle() : enumerator3.Current);
        }
    }
}