using HamsterWheel.Flows.Blocks;

namespace HamsterWheel.Flows.Monitoring;

//Consumer projects register an implementation via DI to observe flow activity
//(e.g. a Web UI showing running flows, progress and failing blocks).
//Implementations must be thread-safe - blocks complete concurrently.
public interface IFlowProgressObserver
{
    void FlowStarted(FlowRunIdentity run, int blockCount);
    void BlockCompleted(FlowRunIdentity run, IPipelineBlock block);
    void BlockFailed(FlowRunIdentity run, IPipelineBlock block, Exception exception);
    void FlowCompleted(FlowRunIdentity run, object? output);
    void FlowFailed(FlowRunIdentity run, Exception error);
}
