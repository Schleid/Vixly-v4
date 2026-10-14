using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using static Vixly.Platform.Domain.Tests.IdentityTestData;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformUserRolesAndDepartmentsTests.cs
//
// PURPOSE:
// Proves role grants (history, expiry, revocation) and
// department memberships on the staff user.
//
// RESPONSIBILITIES:
// - A role is granted once at a time and revoked into history
// - Time-boxed grants stop counting at their expiry
// - The first department is primary; primary moves on leave
//
// IMPORTANT NOTES:
// - Tests assert error codes, never text (L-1)
// =========================================================
public sealed class PlatformUserRolesAndDepartmentsTests
{
    private static readonly Guid Admin = Guid.CreateVersion7(Now);

    private static readonly Guid Role = Guid.CreateVersion7(Now.AddSeconds(1));

    [Fact]
    public void AssignRole_RecordsActor_AndCountsAsInForce()
    {
        PlatformUser user = Active();

        PlatformRoleAssignment grant = user.AssignRole(Role, Admin, null, Now);

        Assert.Equal(Admin, grant.AssignedByUserId);
        Assert.Contains(Role, user.RolesInForce(Now));
    }

    [Fact]
    public void AssignRole_Twice_IsRefused()
    {
        PlatformUser user = Active();
        user.AssignRole(Role, Admin, null, Now);

        AssertError(PlatformIdentityErrors.RoleAlreadyAssigned, () => user.AssignRole(Role, Admin, null, Now));
    }

    [Fact]
    public void RevokeRole_KeepsHistory_AndAllowsANewGrant()
    {
        PlatformUser user = Active();
        user.AssignRole(Role, Admin, null, Now);

        user.RevokeRole(Role, Admin, Now.AddMinutes(1));
        user.AssignRole(Role, Admin, null, Now.AddMinutes(2));

        Assert.Equal(2, user.RoleAssignments.Count);
        Assert.Equal(Admin, user.RoleAssignments[0].RevokedByUserId);
        Assert.Contains(Role, user.RolesInForce(Now.AddMinutes(2)));
    }

    [Fact]
    public void RevokeRole_NotHeld_IsRefused()
    {
        AssertError(PlatformIdentityErrors.RoleNotAssigned, () => Active().RevokeRole(Role, Admin, Now));
    }

    [Fact]
    public void TimeBoxedGrant_StopsAtExpiry()
    {
        PlatformUser user = Active();
        user.AssignRole(Role, Admin, Now.AddHours(1), Now);

        Assert.Contains(Role, user.RolesInForce(Now.AddMinutes(59)));
        Assert.DoesNotContain(Role, user.RolesInForce(Now.AddHours(1)));
    }

    [Fact]
    public void TimeBoxedGrant_MustExpireInTheFuture()
    {
        AssertError(
            PlatformIdentityErrors.AssignmentExpiryNotInFuture,
            () => Active().AssignRole(Role, Admin, Now, Now));
    }

    [Fact]
    public void FirstDepartment_IsPrimary_AndPrimaryMovesOnLeave()
    {
        Guid billing = Guid.CreateVersion7(Now.AddSeconds(2));
        Guid support = Guid.CreateVersion7(Now.AddSeconds(3));
        PlatformUser user = Active();

        user.JoinDepartment(billing, Now);
        user.JoinDepartment(support, Now);

        Assert.True(user.Departments.Single(d => d.PlatformDepartmentId == billing).IsPrimary);
        Assert.False(user.Departments.Single(d => d.PlatformDepartmentId == support).IsPrimary);

        user.LeaveDepartment(billing, Now);

        Assert.True(user.Departments.Single().IsPrimary);
    }

    [Fact]
    public void SetPrimaryDepartment_LeavesExactlyOnePrimary()
    {
        Guid billing = Guid.CreateVersion7(Now.AddSeconds(2));
        Guid support = Guid.CreateVersion7(Now.AddSeconds(3));
        PlatformUser user = Active();
        user.JoinDepartment(billing, Now);
        user.JoinDepartment(support, Now);

        user.SetPrimaryDepartment(support, Now);

        Assert.Single(user.Departments, d => d.IsPrimary);
        Assert.True(user.Departments.Single(d => d.PlatformDepartmentId == support).IsPrimary);
    }

    [Fact]
    public void Departments_RejectDuplicatesAndUnknowns()
    {
        Guid billing = Guid.CreateVersion7(Now.AddSeconds(2));
        PlatformUser user = Active();
        user.JoinDepartment(billing, Now);

        AssertError(PlatformIdentityErrors.AlreadyDepartmentMember, () => user.JoinDepartment(billing, Now));
        AssertError(PlatformIdentityErrors.NotDepartmentMember, () => user.LeaveDepartment(Role, Now));
        AssertError(PlatformIdentityErrors.DepartmentRequired, () => user.JoinDepartment(Guid.Empty, Now));
    }

    private static void AssertError(Error expected, Action action)
    {
        DomainException exception = Assert.Throws<DomainException>(action);
        Assert.Equal(expected, exception.Error);
    }
}
