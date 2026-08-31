namespace HamsterWheel.Flows.Pipelines;

public interface ISuccessHandler
{
    void SetSuccess(bool isSubFlow);
}