using HamsterWheel.Flows.Auth;
using HamsterWheel.Flows.Pipelines;

namespace HamsterWheel.Flows;

public interface IFlowCoordinator : ISuccessHandler
{
    IFlowUserService FlowUserService { get; }
    public void Authorize(IFlow flow);
    void AttachPipeline(IPipeline pipeline, IFlowUserService flowUserService);
}