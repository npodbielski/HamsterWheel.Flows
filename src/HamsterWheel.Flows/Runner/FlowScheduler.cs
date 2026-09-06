using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace HamsterWheel.Flows.Runner;

public partial class FlowScheduler(IServiceProvider services, Channel<IScheduledFlowData> channel, ILogger<IFlowScheduler> logger) : IFlowScheduler
{
    private readonly ConcurrentDictionary<(FlowName, Timer), Timer> _timers = [];
    //serializes timer add+start (Schedule) with remove+stop+dispose (CancelFor),
    //so a cancel can never dispose a timer before it is started
    private readonly Lock _timerLock = new();

    protected bool TriggersLoaded { get; set; }

    public virtual void Schedule(FlowName flowName, Guid userId, string scheduleExpression, object? input = null)
    {
        if (scheduleExpression.IsCronExpression())
        {
            var cron = scheduleExpression.ParseCronExpression();
            var nextOccurence = cron.GetNextOccurrence(DateTime.UtcNow, true);
            if (nextOccurence is null)
            {
                return;
            }

            var dateTimeOffset = new DateTimeOffset(nextOccurence.Value);
            Schedule(flowName, userId, dateTimeOffset,
                () => ScheduleNextCronOccurrence(flowName, userId, scheduleExpression, dateTimeOffset, input),
                input);
        }
        else
        {
            FailedToScheduleFlowWithCronExpression(flowName.ToString(), scheduleExpression);
        }
    }

    //Re-schedules the next occurrence strictly after the boundary that just fired. Anchoring on
    //that boundary (instead of UtcNow) matters because the one-shot timer can fire a few
    //milliseconds early - scheduling from the wall clock would then repeat the same boundary.
    private void ScheduleNextCronOccurrence(FlowName flowName, Guid userId,
        string scheduleExpression, DateTimeOffset lastOccurrence, object? input)
    {
        var nextOccurence = scheduleExpression.ParseCronExpression()
            .GetNextOccurrence(lastOccurrence.UtcDateTime, false);
        if (nextOccurence is null)
        {
            return;
        }

        var dateTimeOffset = new DateTimeOffset(nextOccurence.Value);
        Schedule(flowName, userId, dateTimeOffset,
            () => ScheduleNextCronOccurrence(flowName, userId, scheduleExpression, dateTimeOffset, input),
            input);
    }

    public void Schedule(FlowName flowName, Guid userId, DateTimeOffset at, object? input = null) =>
        Schedule(flowName, userId, at, null, input);

    public void Cancel(FlowName flowName) => CancelFor(key => key.Item1 == flowName);

    public void CancelAll() => CancelFor(null);

    private void CancelFor(Func<(FlowName, Timer), bool>? predicate)
    {
        IEnumerable<(FlowName, Timer)> keys = _timers.Keys;
        if (predicate is not null)
        {
            keys = keys.Where(predicate);
        }

        var timersToStop = new List<Timer>();
        lock (_timerLock)
        {
            foreach (var key in keys)
            {
                if (_timers.TryRemove(key, out var timer))
                {
                    timersToStop.Add(timer);
                }
            }
        }

        foreach (var timer in timersToStop)
        {
            timer.Stop();
            timer.Dispose();
        }
    }

    private void Schedule(FlowName flowName, Guid userId, DateTimeOffset at, Action? scheduleNext, object? input = null)
    {
        var timer = new Timer(at - DateTimeOffset.UtcNow);
        timer.Elapsed += async (_, _) =>
        {
            try
            {
                await channel.Writer.WriteAsync(ScheduledFlowData.New(flowName, userId.ToString(), input));
                scheduleNext?.Invoke();
            }
            catch (Exception e)
            {
                //never crash the thread pool from a fire-and-forget handler
                ScheduleFailure(flowName.ToString(), e);
            }
            finally
            {
                _timers.TryRemove((flowName, timer), out _);
            }
        };
        timer.AutoReset = false;
        lock (_timerLock)
        {
            _timers[(flowName, timer)] = timer;
            timer.Start();
        }
    }

    public virtual async Task LoadTriggers()
    {
        if (!TriggersLoaded)
        {
            await using var scope = services.CreateAsyncScope();
            LoadFlowTriggers(scope.ServiceProvider);
            TriggersLoaded = true;
        }
    }

    private void LoadFlowTriggers(IServiceProvider scopeServiceProvider)
    {
        var cronTriggers = scopeServiceProvider.GetServices<ICronTrigger>().ToArray();
        foreach (var cronTrigger in cronTriggers)
        {
            Schedule(cronTrigger.FlowName, Guid.Empty, cronTrigger.Cron);
        }
    }

    [LoggerMessage(EventId = 0, Level = LogLevel.Warning,
        Message = "Failed to schedule flow: {FlowName} with cron expression: {CronExpression}")]
    public partial void FailedToScheduleFlowWithCronExpression(string flowName, string cronExpression);

    [LoggerMessage(EventId = 1, Level = LogLevel.Error,
        Message = "Scheduled flow {FlowName} failed to run")]
    public partial void ScheduleFailure(string flowName, Exception exception);
}
