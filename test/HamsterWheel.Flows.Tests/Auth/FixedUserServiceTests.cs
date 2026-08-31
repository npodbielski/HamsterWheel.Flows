using FluentAssertions;
using HamsterWheel.Flows.Auth;

namespace HamsterWheel.Flows.Tests.Auth;

public class FixedUserServiceTests
{
    [Fact]
    public void WhenCreatedWithUserId_ThenIdIsSet()
    {
        //arrange
        // (none needed)

        //act
        var service = new FixedUserService("user-123", ["admin"]);

        //assert
        service.Id.Should().Be("user-123");
    }

    [Fact]
    public void WhenCreatedWithPermissions_ThenPermissionsAreSet()
    {
        //arrange
        // (none needed)

        //act
        var service = new FixedUserService("user-1", ["read", "write"]);

        //assert
        service.Permissions.Should().BeEquivalentTo(["read", "write"]);
    }

    [Fact]
    public void WhenCreatedWithNullPermissions_ThenEmptyArray()
    {
        //arrange
        // (none needed)

        //act
        var service = new FixedUserService("user-1", null);

        //assert
        service.Permissions.Should().BeEmpty();
    }

    [Fact]
    public void WhenCreatedWithNullUserId_ThenIdIsNull()
    {
        //arrange
        // (none needed)

        //act
        var service = new FixedUserService(null, null);

        //assert
        service.Id.Should().BeNull();
    }
}

public class UserPermissionsServiceTests
{
    [Fact]
    public void WhenGetPermissions_ThenReturnsEmptyArray()
    {
        //arrange
        var service = new UserPermissionsService();

        //act
        var result = service.GetPermissions("user-1");

        //assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void WhenGetPermissionsWithNullUser_ThenReturnsEmptyArray()
    {
        //arrange
        var service = new UserPermissionsService();

        //act
        var result = service.GetPermissions(null);

        //assert
        result.Should().BeEmpty();
    }
}
