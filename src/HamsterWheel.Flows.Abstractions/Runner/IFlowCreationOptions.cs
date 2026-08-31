namespace HamsterWheel.Flows.Runner;

public interface IFlowCreationOptions
{
    IFlowName? FlowName { get; set; }
    object? Input { get; set; }
}