using HamsterWheel.Flows.IO;
using HamsterWheel.Flows.Pipelines;
using HamsterWheel.HLinq.Data.Converters;

namespace HamsterWheel.Flows.Runner;

internal class FlowApplier(
    IFlowCoordinator coordinator,
    IDefaultConverter converter,
    IFlowGlobalInputBag inputResolver) : IFlowApplier
{
    public async Task Apply(IPipeline pipeline, IFlow flow, object? flowInput)
    {
        coordinator.Authorize(flow);
        var context = CreateContext(pipeline, flow, flowInput);
        await ApplyFlowToPipeline(pipeline, context);
    }

    private async Task ApplyFlowToPipeline(IPipeline pipeline, IFlowContext flowContext)
    {
        ((FlowGlobalInputBag)inputResolver).FromContext(flowContext);
        pipeline.AttachFlowContext(flowContext);
        await flowContext.Flow.ApplyToPipeline(pipeline, inputResolver);
    }

    private FlowContext CreateContext(IPipeline pipeline, IFlow flow, object? flowInput)
    {
        var flowContext = new FlowContext(coordinator)
        {
            Pipeline = pipeline
        };
        var convertedInput = ConvertInput(flow, flowInput);
        flowContext.SetFlow(flow, convertedInput);
        return flowContext;
    }

    private object? ConvertInput(IFlow flow, object? input)
    {
        if (!flow.HaveInput)
        {
            return null;
        }

        IfNullThrow();

        if (input!.GetType() != flow.InputType && flow.InputType is not null)
        {
            try
            {
                input = converter.ConvertTo(flow.InputType, input);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        IfNullThrow();

        if (input is IDataWithValidator validator)
        {
            validator.Validate();
        }

        return input;

        void IfNullThrow()
        {
            if (input == null)
            {
                throw new FlowNoInputException(flow.Name);
            }
        }
    }
}