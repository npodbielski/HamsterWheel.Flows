using HamsterWheel.Flows.Pipelines;

namespace HamsterWheel.Flows;

public abstract class FlowBase<TInput, TOutput> : IFlow where TInput : class
{
    public virtual TimeSpan? AverageTime => null;
    public Type InputType => typeof(TInput);
    public Type OutputType => typeof(TOutput);
    public abstract string Name { get; }
    public bool HaveInput => true;
    public bool HaveOutput => true;
    public virtual bool DoesAllowAnonymousRuns => false;
    public abstract string Definition { get; }
    public virtual string InputSchema => "";
    public virtual string OutputSchema => "";
    public virtual string Version => "1.0";
    public abstract TOutput? BuildTypedOutput();
    public object? BuildOutput() => BuildTypedOutput();

    public async Task ApplyToPipeline(IPipeline pipeline, IFlowGlobalInputBag inputValuesResolver)
    {
        var inputBag = PrepareInputBag(inputValuesResolver);
        await ApplyToPipelineImpl(pipeline, inputBag);
    }

    protected virtual IFlowGlobalInputBag<TInput> PrepareInputBag(IFlowGlobalInputBag inputValuesResolver) =>
        FlowInputValues<TInput>.From(inputValuesResolver);

    protected abstract Task ApplyToPipelineImpl(IPipeline pipeline, IFlowGlobalInputBag<TInput> inputs);
}