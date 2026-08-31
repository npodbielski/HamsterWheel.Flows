namespace HamsterWheel.Flows.Runner;

public interface IPipelineCreationOptions
{
    Guid RunId { get; set; }
    bool ShowLogo { get; set; }
    string? UserId { get; set; }
    bool IsSubFlow { get; set; }
}