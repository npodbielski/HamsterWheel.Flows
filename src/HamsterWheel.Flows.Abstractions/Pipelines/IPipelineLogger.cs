using HamsterWheel.Flows.Monitoring;

namespace HamsterWheel.Flows.Pipelines;

public interface IPipelineLogger
{
    void Log(string message);
    void Log(IFlowLogMessage log);
    void SetSink(Action<IFlowLogMessage> sink);
}