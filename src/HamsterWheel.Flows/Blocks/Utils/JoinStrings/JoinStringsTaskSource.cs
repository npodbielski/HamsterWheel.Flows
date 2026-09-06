using System.Runtime.CompilerServices;
using HamsterWheel.Flows.IO;

namespace HamsterWheel.Flows.Blocks.Utils;

public class JoinStringsTaskSource : IComplexInputSource<JoinStringsInput>
{
    public TaskSource<string?> Delimiter { get; } = new();
    public TaskSource<string> First { get; } = new();
    public TaskSource<string> Second { get; } = new();
    public TaskSource<string> Third { get; } = new();
    public TaskSource<string> Fourth { get; } = new();
    public TaskSource<string> Fifth { get; } = new();
    public TaskSource<IEnumerable<string>> Strings { get; } = new();

    public ITaskSource<JoinStringsInput> EntireInput { get; } = new TaskSource<JoinStringsInput>();

    public bool AllSingle =>
        ((ITaskSource[]) [Delimiter, First, Second, Third, Fourth, Fifth]).All(i => i.IsSingle || !i.IsSet);

    public async IAsyncEnumerable<JoinStringsInput> Get(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (Strings.IsSingle && (!Delimiter.IsSet || Delimiter.IsSingle))
        {
            yield return new JoinStringsInput
            {
                Chunks = [..await Strings.GetSingle()],
                Delimiter = await Delimiter.GetSingle()
            };
            yield break;
        }

        if (First.IsSingle &&
            Second.IsSingle &&
            (Third.IsSingle || !Third.IsSet) &&
            (Fourth.IsSingle || !Fourth.IsSet) &&
            (Fifth.IsSingle || !Fifth.IsSet) &&
            (Delimiter.IsSingle || !Delimiter.IsSet))
        {
            var chunks = new List<string> { await First.GetSingle(), await Second.GetSingle() };
            if (Third.IsSet)
            {
                chunks.Add(await Third.GetSingle());
            }

            if (Fourth.IsSet)
            {
                chunks.Add(await Fourth.GetSingle());
            }

            if (Fifth.IsSet)
            {
                chunks.Add(await Fifth.GetSingle());
            }

            yield return new JoinStringsInput
            {
                Chunks = chunks.ToArray(),
                Delimiter = await Delimiter.GetSingle()
            };
            yield break;
        }

        if (Strings.IsSet)
        {
            var enumerator = Strings.GetMulti().GetAsyncEnumerator(cancellationToken);
            var delimiterEnum = Delimiter.GetMulti().GetAsyncEnumerator(cancellationToken);

            while (await enumerator.MoveNextAsync())
            {
                yield return new JoinStringsInput
                {
                    Chunks = [..enumerator.Current],
                    Delimiter = Delimiter.IsSingle ? await Delimiter.GetSingle() : delimiterEnum.Current
                };
            }
        }

        var enumerator1 = First.GetMulti().GetAsyncEnumerator(cancellationToken);
        var enumerator2 = Second.GetMulti().GetAsyncEnumerator(cancellationToken);
        var enumerator3 = Third.GetMulti().GetAsyncEnumerator(cancellationToken);
        var enumerator4 = Fourth.GetMulti().GetAsyncEnumerator(cancellationToken);
        var enumerator5 = Fifth.GetMulti().GetAsyncEnumerator(cancellationToken);
        var enumerator6 = Delimiter.GetMulti().GetAsyncEnumerator(cancellationToken);

        while (
            (await enumerator1.MoveNextAsync() || First.IsSingle) &&
            (await enumerator2.MoveNextAsync() || Second.IsSingle) &&
            (await enumerator3.MoveNextAsync() || Third.IsSingle || !Third.IsSet) &&
            (await enumerator4.MoveNextAsync() || Fourth.IsSingle || !Fourth.IsSet) &&
            (await enumerator5.MoveNextAsync() || Fifth.IsSingle || !Fifth.IsSet) &&
            (await enumerator6.MoveNextAsync() || Delimiter.IsSingle || !Delimiter.IsSet)
        )
        {
            var chunks = new List<string>
            {
                First.IsSingle ? await First.GetSingle() : enumerator1.Current,
                Second.IsSingle ? await Second.GetSingle() : enumerator2.Current,
            };

            if (Third.IsSet)
            {
                chunks.Add(Third.IsSingle ? await Third.GetSingle() : enumerator3.Current);
            }

            if (Fourth.IsSet)
            {
                chunks.Add(Fourth.IsSingle ? await Fourth.GetSingle() : enumerator4.Current);
            }

            if (Fifth.IsSet)
            {
                chunks.Add(Fifth.IsSingle ? await Fifth.GetSingle() : enumerator5.Current);
            }

            yield return new JoinStringsInput
            {
                Chunks = [.. chunks],
                Delimiter = Delimiter.IsSingle ? await Delimiter.GetSingle() : enumerator6.Current
            };
        }
    }
}
