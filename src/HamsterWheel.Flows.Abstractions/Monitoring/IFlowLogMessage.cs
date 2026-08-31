namespace HamsterWheel.Flows.Monitoring;

public interface IFlowLogMessage
{
    FlowLogLevel Level { get; }
    public string? BlockId { get; } 
    public string? BlockType { get; } 
    DateTimeOffset DateTime { get; }
    string Message { get; }
    Dictionary<string, object> Properties { get; }
}