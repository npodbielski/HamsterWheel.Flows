using HamsterWheel.Flows.Auth;

namespace HamsterWheel.Flows.Pipelines;

public class FlowContext(IFlowCoordinator coordinator) : IFlowContext, ISuccessHandler
{
    private FlowContext(FlowContext previous) : this(previous.Coordinator)
    {
        Flow = previous.Flow;
        Output = previous.Output;
        Input = previous.Input;
    }

    public IFlow Flow { get; private set; } = null!;
    public object? Output { get; private set; }
    public object? Input { get; private set; }
    public IFlowCoordinator Coordinator { get; } = coordinator;
    public IFlowUserService FlowUserService => Coordinator.FlowUserService;
    public required IPipeline Pipeline { get; init; } = null!;

    public void SetFlow(IFlow flow, object? input)
    {
        Flow = flow;
        Output = flow.BuildOutput();
        Input = input;
    }

    public FlowContext GetChildContext() => new(this)
    {
        Pipeline = Pipeline
    };

    public void SetSuccess(bool isSubFlow)
    {
        if (Output is ISuccess output)
        {
            output.Success = true;
        }

        Coordinator.SetSuccess(isSubFlow);
    }
}