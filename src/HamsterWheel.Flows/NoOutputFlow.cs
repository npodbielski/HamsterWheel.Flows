using HamsterWheel.Flows.Pipelines;

namespace HamsterWheel.Flows;

public abstract class NoOutputFlow<TInput> : IFlow where TInput : class
{
    public Type InputType => typeof(TInput);
    public Type? OutputType => null;
    public abstract string Name { get; }
    public bool HaveInput => true;
    public bool HaveOutput => false;
    public object? BuildOutput() => null;
    public virtual string Version => "1.0";
    public virtual TimeSpan? AverageTime => null;
    public virtual bool DoesAllowAnonymousRuns => false;
    public abstract string Definition { get; }
    public virtual string InputSchema => "";
    public string? OutputSchema => null;

    public async Task ApplyToPipeline(IPipeline pipeline, IFlowGlobalInputBag inputValuesResolver)
    {
        var typedInputResolver = FlowInputValues<TInput>.From(inputValuesResolver);
        await ApplyToPipelineImpl(pipeline, typedInputResolver);
    }

    protected abstract Task ApplyToPipelineImpl(IPipeline pipeline, IFlowGlobalInputBag<TInput> inputs);
}