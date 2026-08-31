namespace HamsterWheel.Flows.Pipelines;

public interface IFlowGlobalInputBag : IGlobalInputBag
{
    object? InputAsObject { get; }
}

public interface IFlowGlobalInputBag<out TInput> : IFlowGlobalInputBag
{
    TInput Input { get; }
}