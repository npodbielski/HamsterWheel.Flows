using HamsterWheel.Flows.IO;

namespace HamsterWheel.Flows.Runner;

public interface IScheduledFlowData : IDataWithValidator
{
    Guid ScheduledId { get; }
    DateTimeOffset DateTime { get; }
    IFlowName FlowName { get; }
    string? UserId { get; }
    object? Input { get; }
    bool IsSubFlow { get; }
}