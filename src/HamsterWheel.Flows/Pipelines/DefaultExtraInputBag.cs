namespace HamsterWheel.Flows.Pipelines;

//Hosts provide their own IExtraInputBag to inject extra inputs into flows.
//This default keeps the DI chain resolvable when the host does not provide one
public sealed class DefaultExtraInputBag : IExtraInputBag
{
    public IDictionary<string, object> Bag { get; } = new Dictionary<string, object>();
}
