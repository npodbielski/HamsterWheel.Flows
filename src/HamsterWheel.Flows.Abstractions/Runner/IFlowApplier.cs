using HamsterWheel.Flows.Pipelines;

namespace HamsterWheel.Flows.Runner;

public interface IFlowApplier
{
    Task Apply(IPipeline pipeline, IFlow flow, object? flowInput);
}