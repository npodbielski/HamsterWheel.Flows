using System.Runtime.CompilerServices;
using HamsterWheel.Flows.IO;

namespace HamsterWheel.Flows.Blocks.Os;

public class OsCommandInputTaskSource : IComplexInputSource<OsCommandInput>
{
    public TaskSource<int?> Retries { get; } = new();
    public TaskSource<int> WaitForSeconds { get; } = new(30);
    public TaskSource<string> Command { get; } = new();
    public TaskSource<string?> Arguments { get; } = new();
    public TaskSource<string> WorkingDir { get; } = new();

    public ITaskSource<OsCommandInput> EntireInput { get; } = new TaskSource<OsCommandInput>();
    public bool AllSingle => ((ITaskSource[]) [Retries, WaitForSeconds, Command, Arguments, WorkingDir]).All(i => i.IsSingle || !i.IsSet);

    public async IAsyncEnumerable<OsCommandInput> Get(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (Command.IsSingle &&
            WorkingDir.IsSingle &&
            WaitForSeconds.IsSingle &&
            (Arguments.IsSingle || !Arguments.IsSet) &&
            (Retries.IsSingle || !Retries.IsSet))
        {
            yield return new OsCommandInput(await Command.GetSingle(), await WorkingDir.GetSingle())
            {
                WaitForSeconds = await WaitForSeconds.GetSingle(),
                Arguments = await Arguments.GetSingle(),
                Retries = await Retries.GetSingle(),
            };
            yield break;
        }

        var commandEnum = Command.GetMulti().GetAsyncEnumerator(cancellationToken);
        var workingDirEnum = WorkingDir.GetMulti().GetAsyncEnumerator(cancellationToken);
        var waitEnum = WaitForSeconds.GetMulti().GetAsyncEnumerator(cancellationToken);
        var argumentsEnum = Arguments.GetMulti().GetAsyncEnumerator(cancellationToken);
        var retriesEnum = Retries.GetMulti().GetAsyncEnumerator(cancellationToken);

        while (
            (await commandEnum.MoveNextAsync() || Command.IsSingle) &&
            (await workingDirEnum.MoveNextAsync() || WorkingDir.IsSingle) &&
            (await waitEnum.MoveNextAsync() || WaitForSeconds.IsSingle) &&
            (await argumentsEnum.MoveNextAsync() || Arguments.IsSingle) &&
            (await retriesEnum.MoveNextAsync() || Retries.IsSingle)
        )
        {
            yield return new OsCommandInput(commandEnum.Current!, workingDirEnum.Current!)
            {
                WaitForSeconds = WaitForSeconds.IsSingle ? await WaitForSeconds.GetSingle() : waitEnum.Current,
                Arguments = Arguments.IsSingle ? await Arguments.GetSingle() : argumentsEnum.Current,
                Retries = Retries.IsSingle ? await Retries.GetSingle() : retriesEnum.Current,
            };
        }
    }
}