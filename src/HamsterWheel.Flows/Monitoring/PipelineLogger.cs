using HamsterWheel.Flows.Pipelines;

namespace HamsterWheel.Flows.Monitoring;

public class PipelineLogger : IPipelineLogger
{
    private Action<IFlowLogMessage> _storeLog = l =>
        Console.WriteLine(l.BlockType is not null
        ? $"[{DateTimeOffset.Now:u}] {l.BlockType}:{l.BlockId} || {l.Message}"
        : $"[{DateTimeOffset.Now:u}] || {l.Message}");

    public void Log(IFlowLogMessage log) => _storeLog.Invoke(log);

    public void Log(string message) => _storeLog(new FlowLogMessage(FlowLogLevel.Info, DateTimeOffset.UtcNow, message));

    public void SetSink(Action<IFlowLogMessage> sink) => _storeLog += sink;
}