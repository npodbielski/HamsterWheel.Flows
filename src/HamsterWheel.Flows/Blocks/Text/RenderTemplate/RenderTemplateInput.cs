namespace HamsterWheel.Flows.Blocks.Text;

public class RenderTemplateInput
{
    public required string Template { get; set; } = null!;
    public object? Model { get; set; }
}