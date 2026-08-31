namespace HamsterWheel.Flows.Pipelines;

public interface IExtraInputBag
{
    IDictionary<string, object> Bag { get; }
}