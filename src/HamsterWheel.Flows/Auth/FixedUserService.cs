namespace HamsterWheel.Flows.Auth;

public class FixedUserService(string? userId, string[]? permissions) : IFlowUserService
{
    public string? Id => userId;
    public string[] Permissions => permissions ?? [];
}