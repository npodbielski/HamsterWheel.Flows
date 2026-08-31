namespace HamsterWheel.Flows.Auth;

public interface IUserPermissionsService
{
    public string[] GetPermissions(string? userId);
}