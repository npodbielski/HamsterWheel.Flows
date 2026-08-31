using HamsterWheel.Flows.Runner;

namespace HamsterWheel.Flows.IntegrationTests.Flows;

public sealed class GreetCronTrigger : CronTriggerBase
{
    //fires every 5 seconds (Cronos IncludeSeconds format, 6 fields)
    public override string Cron => "*/5 * * * * *";
    public override FlowName FlowName => new(nameof(GreetFlow));
}
