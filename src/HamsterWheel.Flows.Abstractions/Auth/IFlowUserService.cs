namespace HamsterWheel.Flows.Auth;

public interface IFlowUserService
{
    public string? Id { get; }
    public string[] Permissions { get; }
}