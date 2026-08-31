namespace HamsterWheel.Flows.Blocks.Utils;

public class SetPropertyBlockInput
{
    public object Object { get; set; } = null!;
    public string? PropertyPath { get; set; }
    public object? Value { get; set; }
    public Map? Map { get; set; }
}