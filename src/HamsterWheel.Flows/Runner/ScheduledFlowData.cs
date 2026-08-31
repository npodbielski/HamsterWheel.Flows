using HamsterWheel.Flows.Data.Validation;
using HamsterWheel.Utils;

namespace HamsterWheel.Flows.Runner;

public record ScheduledFlowData(
    Guid ScheduledId,
    DateTimeOffset DateTime,
    IFlowName FlowName,
    string? UserId,
    object? Input,
    bool IsSubFlow) : IScheduledFlowData
{
    public static ScheduledFlowData New(IFlowName flowName, string? userId, object? input) =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, flowName, userId, input, false);

    public static ScheduledFlowData NewSubFlow(IFlowName flowName, string? userId,
        object? input) => new(Guid.NewGuid(), DateTimeOffset.UtcNow, flowName, userId, input, true);

    public void Validate()
    {
        var dataValidator = new DataValidatorFactory().For(this);
        dataValidator.Property(x => x.FlowName).Must(x => x != null!);
        dataValidator.Property(x => x.FlowName.Name).Must(x => !x.IsNullOrWhiteSpace());
    }
}