using HamsterWheel.Utils;

namespace HamsterWheel.Flows;

public record FlowName(string Name, string? Prefix = null) : IFlowName
{
    public const char Separator = ':';
    public override string ToString() => !Prefix.IsNullOrWhiteSpace() ? $"{Prefix}{Separator}{Name}" : Name;

    public static string NormalizeFlowName(string flowName)
    {
        flowName = flowName.ToPascalCase();
        if (!flowName.EndsWith("Flow"))
        {
            flowName += "Flow";
        }

        return flowName;
    }

    public static FlowName FromString(string name)
    {
        if (!name.Contains(Separator))
        {
            return new FlowName(name);
        }

        var chunks = name.Split([Separator], StringSplitOptions.RemoveEmptyEntries);
        return chunks.Length >= 2 ? new FlowName(chunks[1], chunks[2]) : new FlowName(name);
    }
}