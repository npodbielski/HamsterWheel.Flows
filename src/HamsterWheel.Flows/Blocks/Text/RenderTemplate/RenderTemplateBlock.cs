using HamsterWheel.Flows.Blocks.Base;
using HamsterWheel.Flows.DI;
using HamsterWheel.Flows.Templates;
using Microsoft.Extensions.DependencyInjection;

namespace HamsterWheel.Flows.Blocks.Text;

public class RenderTemplateBlock : PipelineBlock<RenderTemplateInput, string, RenderInputTaskSource>, INeedServices
{
    private IRenderingService RenderingService
    {
        get => field ?? throw new BlockServicesNotInitializedException<RenderTemplateBlock>(Id);
        set;
    }

    public override async Task<string> RunForInput(RenderTemplateInput input, CancellationToken token)
    {
        Log("Rendering template...");
        var result = await RenderingService.Render(input.Template, new Dictionary<string, object?>
        {
            ["model"] = input.Model,
            ["input"] = Context.Input
        }, token);
        Log("Done");
        return result;
    }

    public void Resolve(IServiceProvider services) =>
        RenderingService = services.GetRequiredService<IRenderingService>();
}