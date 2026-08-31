using HamsterWheel.Flows.Pipelines;

namespace HamsterWheel.Flows;

public abstract class NoInputFlow<TOutput> : IFlow
{
    public Type? InputType => null;
    public Type OutputType => typeof(TOutput);
    public abstract string Name { get; }
    public bool HaveInput => false;
    public bool HaveOutput => true;

    public virtual string Version => "1.0";

    public virtual TimeSpan? AverageTime => null;
    public virtual bool DoesAllowAnonymousRuns => false;
    public abstract string Definition { get; }
    public string? InputSchema => null;
    public virtual string OutputSchema => "";
    public abstract TOutput? BuildTypedOutput();
    public object? BuildOutput() => BuildTypedOutput();

    public async Task ApplyToPipeline(IPipeline pipeline, IFlowGlobalInputBag inputs) =>
        await ApplyToPipelineImpl(pipeline, inputs);

    protected abstract Task ApplyToPipelineImpl(IPipeline pipeline, IFlowGlobalInputBag inputs);
}