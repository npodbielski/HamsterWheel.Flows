namespace HamsterWheel.Flows.Runner;

public class PipelineCreationOptions : IPipelineCreationOptions
{
    public Guid RunId { get; set; }
    public bool ShowLogo { get; set; }
    public string? UserId { get; set; }
    public bool IsSubFlow { get; set; }
}