namespace HamsterWheel.Flows.Blocks.Utils;

public class SetOutputInput
{
    public string? PropertyPath { get; set; }
    public object? Value { get; set; }
    public Map? Map { get; set; }

    public SetPropertyBlockInput ToParentCommand(object flowOutput) => new()
    {
        PropertyPath = PropertyPath,
        Map = Map,
        Object = flowOutput,
        Value = Value
    };
}