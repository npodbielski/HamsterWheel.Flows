using FluentAssertions;
using HamsterWheel.Flows.Blocks.Utils;
using HamsterWheel.Flows.Monitoring;
using HamsterWheel.Flows.Tests.Utils;

namespace HamsterWheel.Flows.Tests.Infrastructure;

/// <summary>
/// Run log observers: every line of a run reaches them with that run's identity — and only with it,
/// which a sink on the shared pipeline logger cannot guarantee.
/// </summary>
public class FlowRunLogObserverTests
{
    [Fact]
    public void WhenPipelineHasNoRunLogObservers_ThenItsLoggerIsTheHostsLogger()
    {
        //arrange
        var pipeline = new JoinStringsBlock().InitBlock();

        //act & assert
        pipeline.Logger.Should().BeOfType<PipelineLogger>();
    }

    [Fact]
    public void WhenRunLogs_ThenObserverSeesTheLineWithTheIdentityOfItsRun()
    {
        //arrange
        var runId = Guid.NewGuid();
        var observer = new RecordingRunLogObserver();
        var pipeline = new JoinStringsBlock().InitBlock(runLogObservers: [observer], runId: runId);

        //act
        pipeline.Logger.Log("installing module");

        //assert
        var line = observer.Lines.Should().ContainSingle().Subject;
        line.Run.RunId.Should().Be(runId);
        line.Log.Message.Should().Be("installing module");
    }

    [Fact]
    public void WhenTwoRunsLog_ThenNeitherSeesTheLinesOfTheOther()
    {
        //arrange
        var firstRunId = Guid.NewGuid();
        var secondRunId = Guid.NewGuid();
        var firstObserver = new RecordingRunLogObserver();
        var secondObserver = new RecordingRunLogObserver();
        var first = new JoinStringsBlock().InitBlock(runLogObservers: [firstObserver], runId: firstRunId);
        var second = new JoinStringsBlock().InitBlock(runLogObservers: [secondObserver], runId: secondRunId);

        //act
        first.Logger.Log("of the first run");

        //assert
        firstObserver.Lines.Should().ContainSingle().Which.Run.RunId.Should().Be(firstRunId);
        secondObserver.Lines.Should().BeEmpty("a run must not receive the lines of another run, " +
            "nor receive its own twice because another run started in the meantime");
    }

    [Fact]
    public void WhenRunLogs_ThenTheHostsLoggerStillGetsEveryLine()
    {
        //arrange
        var observer = new RecordingRunLogObserver();
        var pipeline = new JoinStringsBlock().InitBlock(runLogObservers: [observer]);
        var written = new List<IFlowLogMessage>();
        pipeline.Logger.SetSink(written.Add);

        //act
        pipeline.Logger.Log("visible to the host too");

        //assert
        written.Should().ContainSingle(m => m.Message == "visible to the host too");
        observer.Lines.Should().ContainSingle(l => l.Log.Message == "visible to the host too");
    }

    [Fact]
    public async Task WhenRunLogObserverThrows_ThenTheLineSurvivesAndTheFlowCompletes()
    {
        //arrange
        var block = new JoinStringsBlock();
        var pipeline = block.InitBlock(runLogObservers: [new ThrowingRunLogObserver()]);
        block.Inputs.First.Const = "a";
        block.Inputs.Second.Const = "b";
        block.Inputs.Delimiter.Const = "-";
        var written = new List<IFlowLogMessage>();
        pipeline.Logger.SetSink(written.Add);

        //act
        var act = async () => await pipeline.Run().WaitSeconds(5);

        //assert
        await act.Should().NotThrowAsync("a faulting observer must not break the flow");
        written.Should().NotContain(m => m.Message.Contains(nameof(IFlowRunLogObserver)),
            "the block's own lines are not blamed on the observer");
        written.Should().Contain(m => m.Message.Contains("Flow run log observer failed"),
            "the observer failure is reported through the logger, once per line it refused to spoil");
    }

    [Fact]
    public void WhenSubFlowLogs_ThenItsIdentitySaysSo()
    {
        //arrange
        var runId = Guid.NewGuid();
        var observer = new RecordingRunLogObserver();
        var pipeline = new JoinStringsBlock().InitBlock(runLogObservers: [observer], runId: runId,
            isSubFlow: true);

        //act
        pipeline.Logger.Log("of a sub-flow");

        //assert
        var line = observer.Lines.Should().ContainSingle().Subject;
        line.Run.RunId.Should().Be(runId);
        line.Run.IsSubFlow.Should().BeTrue(
            "a host must be able to tell a sub-flow's lines from those of the run it was started by");
    }

    private record ObservedLine(FlowRunIdentity Run, IFlowLogMessage Log);

    private class RecordingRunLogObserver : IFlowRunLogObserver
    {
        public List<ObservedLine> Lines { get; } = [];

        public void Log(FlowRunIdentity run, IFlowLogMessage log) => Lines.Add(new ObservedLine(run, log));
    }

    private class ThrowingRunLogObserver : IFlowRunLogObserver
    {
        public void Log(FlowRunIdentity run, IFlowLogMessage log) => throw new InvalidOperationException("boom");
    }
}
