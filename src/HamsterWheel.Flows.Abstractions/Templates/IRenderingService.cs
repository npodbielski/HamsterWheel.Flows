namespace HamsterWheel.Flows.Templates;

public interface IRenderingService
{
    Task<string> Render(string template, Dictionary<string, object?> models, CancellationToken token = default);
}