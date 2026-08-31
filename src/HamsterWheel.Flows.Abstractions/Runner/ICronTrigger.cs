namespace HamsterWheel.Flows.Runner;

public interface ICronTrigger : IOperationTrigger
{
    public string Cron { get; }
    public FlowName FlowName { get; }
}