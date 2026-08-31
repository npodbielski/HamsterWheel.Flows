using HamsterWheel.Flows.Blocks.Base;
using HamsterWheel.Flows.DI;
using HamsterWheel.Flows.Runner;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.Flows.Blocks;

public class SubFlowBlock : PipelineBlock<SubFlowBlockInput, object?, SubFlowTaskSource>, INeedServices
{
    private IFlowRunner FlowRunner
    {
        get => field ?? throw new BlockServicesNotInitializedException<SubFlowBlock>(Id);
        set;
    }

    public override async Task<object?> RunForInput(SubFlowBlockInput input, CancellationToken token) =>
        await FlowRunner.RunAsync(ToMessage(input), token);

    private IScheduledFlowData ToMessage(SubFlowBlockInput input) =>
        ScheduledFlowData.NewSubFlow(FlowName.FromString(input.FlowName), Context.Coordinator.FlowUserService.Id,
            input.Input);

    public void Resolve(IServiceProvider services) =>
        FlowRunner = services.GetRequiredService<IFlowRunner>();
}