using System.Runtime.CompilerServices;
using HamsterWheel.Flows.IO;

namespace HamsterWheel.Flows.Blocks.Os.IO;

public class CopyFilesInputTaskSource : IComplexInputSource<CopyFilesInput>
{
    public CopyFilesInputTaskSource()
    {
        Recursive.Const = true;
        Filter.Const = null;
    }

    public TaskSource<string> SourcePath { get; } = new();
    public TaskSource<string> TargetPath { get; } = new();
    public TaskSource<bool> Recursive { get; } = new();
    public TaskSource<string?> Filter { get; } = new();

    public ITaskSource<CopyFilesInput> EntireInput { get; } = new TaskSource<CopyFilesInput>();

    public bool AllSingle =>
        ((ITaskSource[]) [SourcePath, TargetPath, Recursive, Filter]).All(i => i.IsSingle || !i.IsSet);

    public async IAsyncEnumerable<CopyFilesInput> Get(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (SourcePath.IsSingle && TargetPath.IsSingle && Recursive.IsSingle && Filter.IsSingle)
        {
            yield return new CopyFilesInput
            {
                SourcePath = await SourcePath.GetSingle(),
                TargetPath = await TargetPath.GetSingle(),
                Recursive = await Recursive.GetSingle(),
                Filter = await Filter.GetSingle(),
            };
            yield break;
        }

        var sourcePathCollection = SourcePath.GetMulti().GetAsyncEnumerator(cancellationToken);
        var targetPathCollection = TargetPath.GetMulti().GetAsyncEnumerator(cancellationToken);
        var recursiveCollection = Recursive.GetMulti().GetAsyncEnumerator(cancellationToken);
        var filterCollection = Filter.GetMulti().GetAsyncEnumerator(cancellationToken);

        while ((await sourcePathCollection.MoveNextAsync() || SourcePath.IsSingle) &&
               (await targetPathCollection.MoveNextAsync() || TargetPath.IsSingle) &&
               (await recursiveCollection.MoveNextAsync() || Recursive.IsSingle) &&
               (await filterCollection.MoveNextAsync() || Filter.IsSingle)
              )
        {
            yield return new CopyFilesInput
            {
                SourcePath = SourcePath.IsSingle ? await SourcePath.GetSingle() : sourcePathCollection.Current,
                TargetPath = TargetPath.IsSingle ? await TargetPath.GetSingle() : targetPathCollection.Current,
                Recursive = Recursive.IsSingle ? await Recursive.GetSingle() : recursiveCollection.Current,
                Filter = Filter.IsSingle ? await Filter.GetSingle() : filterCollection.Current,
            };
        }
    }
}