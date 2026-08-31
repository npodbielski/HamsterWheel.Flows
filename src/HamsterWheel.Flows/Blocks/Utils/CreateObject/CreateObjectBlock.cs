using DynamicAnonymousType;
using HamsterWheel.Flows.Blocks.Base;

namespace HamsterWheel.Flows.Blocks.Utils;

public class CreateObjectBlock : PipelineBlock<CreateObjectInput, object, CreateObjectTaskSource>
{
    public override Task<object> RunForInput(CreateObjectInput input, CancellationToken token)
    {
        var props = GetProps(input);
        var type = DynamicFactory.CreateType(props);
        var instance = Activator.CreateInstance(type);

        type.GetProperty(input.Property1)?.SetValue(instance, input.Object1);
        if (input.Property2 is not null)
        {
            type.GetProperty(input.Property2)?.SetValue(instance, input.Object2);
        }

        if (input.Property3 is not null)
        {
            type.GetProperty(input.Property3)?.SetValue(instance, input.Object3);
        }

        return Task.FromResult(instance ?? throw new CouldNotCreateTypeException(Id, type));
    }
    
    private static IEnumerable<(string, Type)> GetProps(CreateObjectInput input)
    {
        yield return (input.Property1, input.Object1?.GetType() ?? typeof(object));
        if (input.Property2 is not null)
        {
            yield return (input.Property2, input.Object2?.GetType() ?? typeof(object));
        }

        if (input.Property3 is not null)
        {
            yield return (input.Property3, input.Object3?.GetType() ?? typeof(object));
        }
    }
}