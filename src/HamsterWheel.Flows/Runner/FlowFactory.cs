using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.Flows.Runner;

public class FlowFactory(IServiceProvider services) : IFlowFactory
{
    public bool Exists(IFlowName flowName) => ResolveFlowImpl(flowName) != null;

    public IFlow Create(Action<IFlowCreationOptions> configureOptions)
    {
        var options = new FlowCreationOptions();
        configureOptions(options);
        options.Validate();
        return ResolveFlow(options.FlowName);
    }

    private IFlow ResolveFlow(IFlowName flowName) =>
        ResolveFlowImpl(flowName) ?? throw new FlowNotFoundException(flowName);

    protected virtual IFlow? ResolveFlowImpl(IFlowName flowName) =>
        services.GetKeyedService<IFlow>(new FlowName(flowName.Name, flowName.Prefix).Name) ?? null;
}