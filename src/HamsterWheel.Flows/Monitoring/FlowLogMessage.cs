using HamsterWheel.Utils;

namespace HamsterWheel.Flows.Monitoring;

public record FlowLogMessage : IFlowLogMessage
{
    private static readonly string BlockIdKeyName = nameof(BlockId).ToCamelCase();
    private static readonly string BlockTypeKeyName = nameof(BlockType).ToCamelCase();

    public FlowLogMessage(FlowLogLevel Level,
        DateTimeOffset DateTime,
        string Message,
        string? BlockId = null,
        string? BlockType = null,
        Dictionary<string, object>? Properties = null)
    {
        this.Level = Level;
        this.DateTime = DateTime;
        this.Message = Message;
        this.Properties = Properties ?? [];

        if (BlockId is not null)
        {
            this.Properties.Add(BlockIdKeyName, BlockId);
        }

        if (BlockType is not null)
        {
            this.Properties.Add(BlockTypeKeyName, BlockType);
        }
    }

    public Dictionary<string, object> Properties { get; }
    public FlowLogLevel Level { get; }
    public DateTimeOffset DateTime { get; }
    public string Message { get; }

    public string? BlockId => Properties.TryGetValue(BlockIdKeyName, out var value) && value is string blockId
        ? blockId
        : null;

    public string? BlockType => Properties.TryGetValue(BlockTypeKeyName, out var value) && value is string blockType
        ? blockType
        : null;

    public void Deconstruct(out FlowLogLevel level, out DateTimeOffset dateTime, out string message,
        out Dictionary<string, object>? properties)
    {
        level = Level;
        dateTime = DateTime;
        message = Message;
        properties = Properties;
    }
}