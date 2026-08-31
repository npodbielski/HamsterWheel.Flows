using HamsterWheel.Flows.Data.Serialization;
using HamsterWheel.Flows.Templates;

namespace HamsterWheel.Flows.Pipelines;

public class FlowGlobalInputBag(
    IRenderingService renderingService,
    ISerializer serializer,
    IExtraInputBag? extraInputBag)
    : IFlowGlobalInputBag
{
    public IDictionary<string, object> Bag { get; } = extraInputBag?.Bag ?? new Dictionary<string, object>();
    public bool InputAvailable => Flow.HaveInput;
    public object? InputAsObject { get; private set; }
    internal IFlow Flow { get; private set; } = null!;

    public void FromContext(IFlowContext flowContext)
    {
        Flow = flowContext.Flow;
        InputAsObject = flowContext.Input;
    }

    public async Task<string> Render(string inputTemplate)
    {
        var models = new Dictionary<string, object?>();
        if (InputAvailable)
        {
            //TODO: add Levenshtein strings distance to rid of typos thought probably the best place would be to add it to rendering service as fallback if property does not exists
            models["input"] = InputAsObject;
        }

        return await renderingService.Render(inputTemplate, models);
    }

    public string Serialize(object input)
    {
        if (input is string str)
        {
            return str;
        }

        return serializer.Serialize(input);
    }
}

public class FlowInputValues<TInput>(FlowGlobalInputBag untyped) : IFlowGlobalInputBag<TInput>
{
    public IDictionary<string, object> Bag => untyped.Bag;

    public TInput Input =>
        (untyped.InputAsObject switch
        {
            null => (TInput?)(object?)null,
            TInput input => input,
            _ => throw new MismatchedFlowInputTypeException(untyped.Flow.Name, untyped.Flow.InputType)
        })!;
    
    public object? InputAsObject => untyped.InputAsObject;

    public Task<string> Render(string inputTemplate) => untyped.Render(inputTemplate);
    public string Serialize(object input) => untyped.Serialize(input);

    public static FlowInputValues<TInput> From(IFlowGlobalInputBag notTyped) => new((FlowGlobalInputBag)notTyped);
}