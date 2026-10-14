using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using static Vixly.Platform.Domain.Tests.IdentityTestData;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformRoleTests.cs
//
// PURPOSE:
// Proves custom and system role rules.
//
// RESPONSIBILITIES:
// - Custom roles can be renamed, deactivated and edited
// - System roles are protected and only synchronised from
//   code
// - Permission codes are validated and de-duplicated
// - An inactive role grants nothing
//
// IMPORTANT NOTES:
// - Tests assert error codes, never text (L-1)
// =========================================================
public sealed class PlatformRoleTests
{
    [Fact]
    public void CustomRole_ReplacesPermissions_WithoutDuplicates()
    {
        PlatformRole role = PlatformRole.CreateCustom("Billing desk", Now);

        role.ReplacePermissions(new[] { "platform.billing.view", "platform.billing.view", "platform.users.view" }, Now);
        role.ReplacePermissions(new[] { "platform.billing.view", "platform.billing.issue_invoice" }, Now);

        Assert.Equal(
            new[] { "platform.billing.issue_invoice", "platform.billing.view" },
            role.Permissions.Select(p => p.PermissionCode).Order(StringComparer.Ordinal));
        Assert.True(role.Grants("platform.billing.view"));
        Assert.False(role.Grants("platform.users.view"));
    }

    [Theory]
    [InlineData("billing.view")]
    [InlineData("platform")]
    [InlineData("Platform.Billing.View")]
    [InlineData("operations.billing.view")]
    public void InvalidPermissionCode_IsRejected(string code)
    {
        PlatformRole role = PlatformRole.CreateCustom("Billing desk", Now);

        AssertError(PlatformIdentityErrors.PermissionCodeInvalid, () => role.ReplacePermissions(new[] { code }, Now));
    }

    [Fact]
    public void InactiveRole_GrantsNothing()
    {
        PlatformRole role = PlatformRole.CreateCustom("Billing desk", Now);
        role.ReplacePermissions(new[] { "platform.billing.view" }, Now);

        role.Deactivate(Now);

        Assert.False(role.Grants("platform.billing.view"));
        AssertError(PlatformIdentityErrors.RoleAlreadyInactive, () => role.Deactivate(Now));
    }

    [Fact]
    public void SystemRole_IsProtected_ButSynchronisedFromCode()
    {
        PlatformRole role = PlatformRole.CreateSystem("security_administrator", Now);

        AssertError(PlatformIdentityErrors.SystemRoleProtected, () => role.Rename("x", Now));
        AssertError(PlatformIdentityErrors.SystemRoleProtected, () => role.Deactivate(Now));
        AssertError(
            PlatformIdentityErrors.SystemRoleProtected,
            () => role.ReplacePermissions(new[] { "platform.users.view" }, Now));

        role.SynchronizeSystemPermissions(new[] { "platform.users.view" }, Now);

        Assert.True(role.IsSystem);
        Assert.True(role.Grants("platform.users.view"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Security Admin")]
    [InlineData("9admin")]
    public void SystemRole_KeyMustBeSnakeCase(string key)
    {
        AssertError(PlatformIdentityErrors.SystemRoleKeyInvalid, () => PlatformRole.CreateSystem(key, Now));
    }

    [Fact]
    public void CustomRole_CannotBeSynchronisedAsSystem()
    {
        PlatformRole role = PlatformRole.CreateCustom("Billing desk", Now);

        AssertError(
            PlatformIdentityErrors.SystemRoleKeyInvalid,
            () => role.SynchronizeSystemPermissions(new[] { "platform.users.view" }, Now));
    }

    [Fact]
    public void Department_DeactivatesAndReactivatesOnce()
    {
        PlatformDepartment department = PlatformDepartment.Create(" Billing ", "  ", Now);

        department.Deactivate(Now);

        Assert.Equal("Billing", department.Name);
        Assert.Null(department.Description);
        AssertError(PlatformIdentityErrors.DepartmentAlreadyInactive, () => department.Deactivate(Now));

        department.Reactivate(Now);

        AssertError(PlatformIdentityErrors.DepartmentAlreadyActive, () => department.Reactivate(Now));
    }

    private static void AssertError(Error expected, Action action)
    {
        DomainException exception = Assert.Throws<DomainException>(action);
        Assert.Equal(expected, exception.Error);
    }
}
