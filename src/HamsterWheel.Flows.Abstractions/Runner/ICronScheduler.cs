namespace HamsterWheel.Flows.Runner;

public interface IFlowScheduler
{
    void Schedule(FlowName flowName, Guid userId, string scheduleExpression, object? input = null);
    void Schedule(FlowName flowName, Guid userId, DateTimeOffset at, object? input = null);
    void Cancel(FlowName flowName);
    void CancelAll();
    Task LoadTriggers();
}