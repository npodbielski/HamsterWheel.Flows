using HamsterWheel.Flows.Auth;
using HamsterWheel.Flows.Blocks;
using HamsterWheel.Flows.Data.Serialization;
using HamsterWheel.Flows.DI;
using HamsterWheel.Flows.Monitoring;
using HamsterWheel.Flows.Pipelines;
using HamsterWheel.Flows.Runner;
using HamsterWheel.Flows.Templates;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
// using HamsterWheel.Platform.Options;
// using HamsterWheel.Tests.Common;

namespace HamsterWheel.Flows.Tests;

public static class BlockTestHelper
{
    public static Pipeline InitBlock(this IPipelineBlock block, object? output = null,
        Action<IServiceCollection>? configureServices = null, IFlowProgressObserver? observer = null) =>
        ((IPipelineBlock[])[block]).InitBlock(output, configureServices, observer);

    public static Pipeline InitBlock(this IPipelineBlock[] blocks, object? output = null,
        Action<IServiceCollection>? configureServices = null, IFlowProgressObserver? observer = null)
    {
        var flowCoordinator = new FlowCoordinator();
        var blockFactory = Substitute.For<IBlockFactory>();
        // var optionsAccessor = Substitute.For<IOptionsAccessor>();
        // optionsAccessor.Get().Returns(TestPlatformOptions.Default);
        var renderingService = Substitute.For<IRenderingService>();
        var flowUserService = Substitute.For<IFlowUserService>();
        var serializerService = Substitute.For<ISerializer>();
        var pipelineLogger = new PipelineLogger();
        var extraInputs = Substitute.For<IExtraInputBag>();

        var flow = Substitute.For<IFlow>();
        flow.DoesAllowAnonymousRuns.Returns(true);
        flow.HaveOutput.Returns(true);
        flow.BuildOutput().Returns(output);

        var pipeline = new Pipeline(new PipelineCreationOptions { IsSubFlow = false }, flowCoordinator, blockFactory,
            pipelineLogger, observer);
        flowCoordinator.AttachPipeline(pipeline, flowUserService);
        flowCoordinator.Authorize(flow);

        var context = new FlowContext(flowCoordinator)
        {
            Pipeline = pipeline
        };
        context.SetFlow(flow, null);
        pipeline.AttachFlowContext(context);
        flow.ApplyToPipeline(pipeline, new FlowGlobalInputBag(renderingService, serializerService, extraInputs));
        foreach (var block in blocks)
        {
            pipeline.AddBlock(block);
            SetServices(block, configureServices);
        }

        return pipeline;
    }

    private static void SetServices(IPipelineBlock block, Action<IServiceCollection>? configureServices)
    {
        if (block is not INeedServices resolver || configureServices is null)
        {
            return;
        }

        var services = new ServiceCollection();
        configureServices.Invoke(services);
        resolver.Resolve(services.BuildServiceProvider());
    }
}
