namespace HamsterWheel.Flows.Runner;

public abstract class CronTriggerBase : ICronTrigger
{
    public abstract string Cron { get; }

    public abstract FlowName FlowName { get; }

    //TODO: user id and cron schedule should be stored in DB in order to overwrite this 
    public Guid UserId { get; set; }
}