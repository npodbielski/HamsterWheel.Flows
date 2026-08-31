using System.Runtime.CompilerServices;
using HamsterWheel.Flows.IO;

namespace HamsterWheel.Flows.Blocks;

public class SubFlowTaskSource : IComplexInputSource<SubFlowBlockInput>
{
    public TaskSource<string> FlowName { get; } = new();
    public TaskSource<object?> Input { get; } = new();

    public ITaskSource<SubFlowBlockInput> EntireInput { get; } = new TaskSource<SubFlowBlockInput>();
    public bool AllSingle => ((ITaskSource[]) [FlowName, Input]).All(i => i.IsSingle || !i.IsSet);

    public async IAsyncEnumerable<SubFlowBlockInput> Get(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (FlowName.IsSingle && (Input.IsSingle || !Input.IsSet))
        {
            yield return new SubFlowBlockInput
            {
                FlowName = await FlowName.GetSingle(),
                Input = await Input.GetSingle()
            };
            yield break;
        }

        var nameEnumerator = FlowName.GetMulti().GetAsyncEnumerator(cancellationToken);
        var inputEnumerator = Input.GetMulti().GetAsyncEnumerator(cancellationToken);

        while (await nameEnumerator.MoveNextAsync() && ( await inputEnumerator.MoveNextAsync() || !Input.IsSet))
        {
            yield return new SubFlowBlockInput
            {
                FlowName = nameEnumerator.Current!,
                Input = inputEnumerator.Current
            };
        }
    }
}