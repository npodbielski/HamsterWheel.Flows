namespace HamsterWheel.Flows.Runner;

public class FlowRunner(IPipelineFactory pipelineFactory, IFlowFactory flowFactory, IFlowApplier flowApplier)
    : IFlowRunner
{
    public async Task<IFlowRunResult> RunAsync(IScheduledFlowData message, CancellationToken token)
    {
        message.Validate();

        var pipeline = pipelineFactory.Create(c =>
        {
            c.RunId = message.ScheduledId;
            c.UserId = message.UserId;
            c.ShowLogo = !message.IsSubFlow;
            c.IsSubFlow = message.IsSubFlow;
        });

        var flow = flowFactory.Create(c => c.FlowName = message.FlowName);
        await flowApplier.Apply(pipeline, flow, message.Input);

        var result = await pipeline.Run(token);
        return new FlowRunResult(message.FlowName, flow.HaveOutput ? result : null);
    }
}