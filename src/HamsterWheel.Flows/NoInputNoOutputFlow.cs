using HamsterWheel.Flows.Pipelines;

namespace HamsterWheel.Flows;

public abstract class NoInputNoOutputFlow : IFlow
{
    public Type? InputType => null;
    public Type? OutputType => null;
    public abstract string Name { get; }
    public bool HaveInput => false;
    public bool HaveOutput => false;
    public object? BuildOutput() => null;
    public virtual string Version => "1.0";
    public virtual TimeSpan? AverageTime => null;
    public virtual bool DoesAllowAnonymousRuns => false;
    public abstract string Definition { get; }
    public string? InputSchema => null;
    public string? OutputSchema => null;

    public async Task ApplyToPipeline(IPipeline pipeline, IFlowGlobalInputBag inputs) =>
        await ApplyToPipelineImpl(pipeline, inputs);

    protected abstract Task ApplyToPipelineImpl(IPipeline pipeline, IFlowGlobalInputBag inputs);
}