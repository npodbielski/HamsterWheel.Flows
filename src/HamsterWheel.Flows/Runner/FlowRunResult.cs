namespace HamsterWheel.Flows.Runner;

public record FlowRunResult(IFlowName FlowName, object? Output) : IFlowRunResult;