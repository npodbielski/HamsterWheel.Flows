namespace HamsterWheel.Flows.Blocks.Utils;

public class JoinStringsInput
{
    public required string[] Chunks { get; init; }
    public string? Delimiter { get; set; }
}