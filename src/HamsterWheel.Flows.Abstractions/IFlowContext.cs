using HamsterWheel.Flows.Auth;
using HamsterWheel.Flows.Pipelines;

namespace HamsterWheel.Flows;

public interface IFlowContext
{
    IPipeline Pipeline { get; }
    object? Output { get; }
    object? Input { get; }
    IFlow Flow { get; }
    IFlowCoordinator Coordinator { get; }
    IFlowUserService FlowUserService { get; }
}