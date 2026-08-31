namespace HamsterWheel.Flows.Pipelines;

public interface IGlobalInputBag
{
    IDictionary<string, object> Bag { get; }
    Task<string> Render(string inputTemplate);
    string Serialize(object input);
}