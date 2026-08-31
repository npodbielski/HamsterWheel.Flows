using HamsterWheel.Flows.Pipelines;

namespace HamsterWheel.Flows.Runner;

public interface IPipelineFactory
{
    IPipeline Create(Action<IPipelineCreationOptions> configureOptions);
}