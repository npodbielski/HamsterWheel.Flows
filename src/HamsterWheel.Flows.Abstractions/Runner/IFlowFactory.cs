namespace HamsterWheel.Flows.Runner;

public interface IFlowFactory
{
    public IFlow Create(Action<IFlowCreationOptions> configureOptions);
    bool Exists(IFlowName flowName);
}