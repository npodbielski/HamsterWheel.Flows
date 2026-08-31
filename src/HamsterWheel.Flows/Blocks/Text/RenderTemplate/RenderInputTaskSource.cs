using System.Runtime.CompilerServices;
using HamsterWheel.Flows.IO;

namespace HamsterWheel.Flows.Blocks.Text;

public class RenderInputTaskSource : IComplexInputSource<RenderTemplateInput>
{
    public TaskSource<string> Template { get; } = new();
    public TaskSource<object?> Model { get; } = new();

    public ITaskSource<RenderTemplateInput> EntireInput { get; } = new TaskSource<RenderTemplateInput>();
    public bool AllSingle => ((ITaskSource[]) [Template, Model]).All(i => i.IsSingle || !i.IsSet);

    public async IAsyncEnumerable<RenderTemplateInput> Get([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (Template.IsSingle && (Model.IsSingle || !Model.IsSet))
        {
            yield return new RenderTemplateInput
            {
                Template = await Template.GetSingle(),
                Model = await Model.GetSingle()
            };
            yield break;
        }

        var templateEnum = Template.GetMulti().GetAsyncEnumerator(cancellationToken);
        var modelEnum = Model.GetMulti().GetAsyncEnumerator(cancellationToken);

        while ((await templateEnum.MoveNextAsync() || Template.IsSingle) &&
               (await modelEnum.MoveNextAsync() || Model.IsSingle))
        {
            yield return new RenderTemplateInput
            {
                Template = Template.IsSingle ? await Template.GetSingle() : templateEnum.Current,
                Model = Model.IsSingle ? await Model.GetSingle() : modelEnum.Current,
            };
        }
    }
}