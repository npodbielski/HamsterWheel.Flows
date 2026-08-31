using HamsterWheel.Flows.Auth;
using HamsterWheel.Flows.Pipelines;
using HamsterWheel.Utils;

namespace HamsterWheel.Flows;

public class FlowCoordinator : IFlowCoordinator
{
    public List<IPipeline> ActivePipelines { get; } = [];
    public IFlowUserService FlowUserService { get; private set; } = null!;

    public void AttachPipeline(IPipeline pipeline, IFlowUserService flowUserService)
    {
        ActivePipelines.Add(pipeline);
        FlowUserService = flowUserService;
        pipeline.AttachCoordinator(this);
    }

    public void Authorize(IFlow flow)
    {
        var flowUserService = FlowUserService ?? throw new DuringFlowAttachMissingUserException();
        CheckUser(flow, flowUserService);
    }

    public virtual void SetSuccess(bool isSubFlow)
    {
    }

    private static void CheckUser(IFlow flow, IFlowUserService flowUserService)
    {
        if (!flow.DoesAllowAnonymousRuns && flowUserService.Id.IsNullOrWhiteSpace())
        {
            throw new UserRequiredToRunFlowException(flow);
        }
    }
}