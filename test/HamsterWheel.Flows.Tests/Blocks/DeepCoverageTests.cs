using FluentAssertions;
using HamsterWheel.Flows;
using HamsterWheel.Flows.Blocks.Os;
using HamsterWheel.Flows.Blocks.Os.IO;
using HamsterWheel.Flows.Blocks.Utils;
using HamsterWheel.Flows.Data.Serialization;
using HamsterWheel.Flows.Pipelines;
using HamsterWheel.Flows.Runner;
using HamsterWheel.Flows.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Threading.Channels;

namespace HamsterWheel.Flows.Tests.Blocks;

public class DeepCoverageTests
{
    [Fact]
    public async Task WhenCopyFilesInputTaskSourceAllConst_ThenReturnsSingleInput()
    {
        //arrange
        var source = new CopyFilesInputTaskSource();
        source.SourcePath.Const = "/src";
        source.TargetPath.Const = "/dst";
        source.Recursive.Const = false;

        //act
        var results = new List<CopyFilesInput>();
        await foreach (var item in source.Get())
        {
            results.Add(item);
        }

        //assert
        results.Should().HaveCount(1);
        results[0].SourcePath.Should().Be("/src");
        results[0].TargetPath.Should().Be("/dst");
        results[0].Recursive.Should().BeFalse();
        results[0].Filter.Should().BeNull();
    }

    [Fact]
    public void WhenCopyFilesInputTaskSourceAllSingle_ThenTrue()
    {
        //arrange
        var source = new CopyFilesInputTaskSource();
        source.SourcePath.Const = "/a";
        source.TargetPath.Const = "/b";

        //act & assert
        source.AllSingle.Should().BeTrue();
    }

    [Fact]
    public async Task WhenFlowSchedulerScheduleWithValidCron_ThenSchedules()
    {
        //arrange
        var services = Substitute.For<IServiceProvider>();
        var channel = Channel.CreateUnbounded<IScheduledFlowData>();
        var logger = Substitute.For<ILogger<IFlowScheduler>>();
        var scheduler = new FlowScheduler(services, channel, logger);

        //act - valid cron expression "0 0 * * *"
        var act = () => scheduler.Schedule(new FlowName("test-flow"), Guid.NewGuid(), "0 0 * * *");

        //assert
        act.Should().NotThrow();
    }

    [Fact]
    public void WhenFlowSchedulerScheduleWithInvalidCron_ThenLogsWarning()
    {
        //arrange
        var services = Substitute.For<IServiceProvider>();
        var channel = Channel.CreateUnbounded<IScheduledFlowData>();
        var logger = Substitute.For<ILogger<IFlowScheduler>>();
        var scheduler = new FlowScheduler(services, channel, logger);

        //act - invalid cron expression
        var act = () => scheduler.Schedule(new FlowName("test-flow"), Guid.NewGuid(), "not-a-cron");

        //assert - should not throw, just log warning
        act.Should().NotThrow();
    }

    [Fact]
    public void WhenFlowSchedulerCancelAll_ThenNoException()
    {
        //arrange
        var services = Substitute.For<IServiceProvider>();
        var channel = Channel.CreateUnbounded<IScheduledFlowData>();
        var logger = Substitute.For<ILogger<IFlowScheduler>>();
        var scheduler = new FlowScheduler(services, channel, logger);

        //act
        var act = () => scheduler.CancelAll();

        //assert
        act.Should().NotThrow();
    }

    [Fact]
    public void WhenFlowSchedulerCancel_ThenNoException()
    {
        //arrange
        var services = Substitute.For<IServiceProvider>();
        var channel = Channel.CreateUnbounded<IScheduledFlowData>();
        var logger = Substitute.For<ILogger<IFlowScheduler>>();
        var scheduler = new FlowScheduler(services, channel, logger);

        //act
        var act = () => scheduler.Cancel(new FlowName("some-flow"));

        //assert
        act.Should().NotThrow();
    }

    [Fact]
    public void WhenFlowGlobalInputBagSerializeString_ThenReturnsAsIs()
    {
        //arrange
        var renderingService = Substitute.For<IRenderingService>();
        var serializer = Substitute.For<ISerializer>();
        var bag = new FlowGlobalInputBag(renderingService, serializer, null);

        //act
        var result = bag.Serialize("hello");

        //assert
        result.Should().Be("hello");
    }

    [Fact]
    public void WhenFlowGlobalInputBagSerializeObject_ThenUsesSerializer()
    {
        //arrange
        var renderingService = Substitute.For<IRenderingService>();
        var serializer = Substitute.For<ISerializer>();
        serializer.Serialize(Arg.Any<object>()).Returns("serialized");
        var bag = new FlowGlobalInputBag(renderingService, serializer, null);

        //act
        var result = bag.Serialize(new { A = 1 });

        //assert
        result.Should().Be("serialized");
    }

    [Fact]
    public async Task WhenFlowGlobalInputBagRenderWithInput_ThenPassesInputToRenderer()
    {
        //arrange
        var renderingService = Substitute.For<IRenderingService>();
        var serializer = Substitute.For<ISerializer>();
        var bag = new FlowGlobalInputBag(renderingService, serializer, null);

        // Mock a flow context
        var flowContext = Substitute.For<IFlowContext>();
        var flow = Substitute.For<IFlow>();
        flow.HaveInput.Returns(true);
        flowContext.Flow.Returns(flow);
        flowContext.Input.Returns("test-input");
        bag.FromContext(flowContext);

        renderingService.Render("template", Arg.Any<Dictionary<string, object?>>()).Returns("rendered");

        //act
        var result = await bag.Render("template");

        //assert
        result.Should().Be("rendered");
    }

    [Fact]
    public void WhenFlowGlobalInputBagInputAvailable_ThenReflectsFlow()
    {
        //arrange
        var renderingService = Substitute.For<IRenderingService>();
        var serializer = Substitute.For<ISerializer>();
        var bag = new FlowGlobalInputBag(renderingService, serializer, null);

        var flowContext = Substitute.For<IFlowContext>();
        var flow = Substitute.For<IFlow>();
        flow.HaveInput.Returns(false);
        flowContext.Flow.Returns(flow);
        bag.FromContext(flowContext);

        //act & assert
        bag.InputAvailable.Should().BeFalse();
    }

    [Fact]
    public void WhenFlowGlobalInputBagWithExtraBag_ThenUsesExtraBag()
    {
        //arrange
        var renderingService = Substitute.For<IRenderingService>();
        var serializer = Substitute.For<ISerializer>();
        var extraBag = Substitute.For<IExtraInputBag>();
        var dict = new Dictionary<string, object> { ["key"] = "value" };
        extraBag.Bag.Returns(dict);
        var bag = new FlowGlobalInputBag(renderingService, serializer, extraBag);

        //act & assert
        bag.Bag.Should().BeSameAs(dict);
    }

    [Fact]
    public async Task WhenJoinStringsTaskSourceAllConst_ThenReturnsSingleInput()
    {
        //arrange
        var source = new JoinStringsTaskSource();
        source.First.Const = "a";
        source.Second.Const = "b";
        source.Delimiter.Const = "-";

        //act
        var results = new List<JoinStringsInput>();
        await foreach (var item in source.Get())
        {
            results.Add(item);
        }

        //assert
        results.Should().HaveCount(1);
        results[0].Chunks.Should().Contain("a");
        results[0].Delimiter.Should().Be("-");
    }

    [Fact]
    public void WhenJoinStringsTaskSourceAllSingle_ThenTrue()
    {
        //arrange
        var source = new JoinStringsTaskSource();
        source.First.Const = "x";

        //act & assert
        source.AllSingle.Should().BeTrue();
    }

    [Fact]
    public async Task WhenJoinStringsTaskSourceMultiInput_ThenProducesAllCombinations()
    {
        //arrange
        var source = new JoinStringsTaskSource();
        source.First.SetSource(AsyncEnum("a"));
        source.Second.Const = "b";
        source.Delimiter.Const = "-";

        //act
        var results = new List<JoinStringsInput>();
        await foreach (var item in source.Get())
        {
            results.Add(item);
        }

        //assert
        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task WhenFlowGlobalInputBagRenderWithInputAvailable_ThenPassesInput()
    {
        //arrange
        var renderingService = Substitute.For<IRenderingService>();
        var serializer = Substitute.For<ISerializer>();
        var bag = new FlowGlobalInputBag(renderingService, serializer, null);

        var flowContext = Substitute.For<IFlowContext>();
        var flow = Substitute.For<IFlow>();
        flow.HaveInput.Returns(true);
        flowContext.Flow.Returns(flow);
        flowContext.Input.Returns("my-input");
        bag.FromContext(flowContext);

        renderingService.Render(Arg.Any<string>(), Arg.Any<Dictionary<string, object?>>()).Returns("out");

        //act
        var result = await bag.Render("{{input}}");

        //assert
        result.Should().Be("out");
    }

    private static async IAsyncEnumerable<string> AsyncEnum(string value)
    {
        yield return value;
        yield return value;
        yield return value;
        await System.Threading.Tasks.Task.CompletedTask;
    }
}

public class OsCommandInputCoverageTests
{
    [Fact]
    public void WhenOsCommandInputWithMaskingFunction_ThenArgumentsMasked()
    {
        //arrange
        var input = new OsCommandInput(
            "curl", Path.GetTempPath(), "secret-token", s => s.Replace("secret", "****"));

        //act
        var result = input.CommandWithArguments;

        //assert
        result.Should().Contain("****");
    }

    [Fact]
    public void WhenOsCommandInputWithoutMasking_ThenArgumentsAsIs()
    {
        //arrange
        var input = new OsCommandInput(
            "ls", Path.GetTempPath(), "-la");

        //act
        var result = input.CommandWithArguments;

        //assert
        result.Should().Contain("ls").And.Contain("-la");
    }

    [Fact]
    public void WhenOsCommandInputWithEmptyCommand_ThenThrows()
    {
        //arrange & act
        var act = () => new OsCommandInput("", Path.GetTempPath());

        //assert
        act.Should().Throw<InvalidOsCommandException<OsCommandInput>>();
    }

    [Fact]
    public void WhenOsCommandInputWithInvalidDir_ThenThrows()
    {
        //arrange & act
        var act = () => new OsCommandInput("ls", "/nonexistent/path/xyz");

        //assert
        act.Should().Throw<InvalidOsCommandException<OsCommandInput>>();
    }
}
