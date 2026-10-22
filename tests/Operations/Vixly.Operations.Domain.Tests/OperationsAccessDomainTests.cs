using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsAccessDomainTests.cs
//
// PURPOSE:
// Proves the roles, grants and staff status rules of Slice 1D.
//
// RESPONSIBILITIES:
// - Roles hold known permissions only; system-only permissions are reserved
// - The Administrator role cannot be changed
// - Grants need a valid scope; revoking is idempotent
// - Status changes need a reason; the Owner is protected
// - Ownership goes only to an active Administrator with a second factor
//
// IMPORTANT NOTES:
// - No database
// =========================================================
public sealed class OperationsAccessDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid Tenant = Guid.NewGuid();

    [Fact]
    public void Role_KeepsKnownPermissions_InCatalogueOrder()
    {
        OperationsRole role = OperationsRole.Create(Tenant, "  Dispatcher ", new[] { OperationsPermissions.BrandView, OperationsPermissions.StaffView, OperationsPermissions.StaffView, " " }, Now);

        Assert.Equal("Dispatcher", role.Name);
        Assert.Equal(new[] { OperationsPermissions.StaffView, OperationsPermissions.BrandView }, role.Permissions);
        Assert.True(role.Holds(OperationsPermissions.StaffView));
        Assert.False(role.Holds(OperationsPermissions.StaffInvite));

        role.SetActive(false, Now);
        Assert.False(role.Holds(OperationsPermissions.StaffView));
    }

    [Theory]
    [InlineData("ops.devices.approve", "ops.roles.permission_reserved")]
    [InlineData("ops.devices.release", "ops.roles.permission_reserved")]
    [InlineData("ops.company.request_change", "ops.roles.permission_reserved")]
    [InlineData("ops.staff.everything", "ops.roles.permission_unknown")]
    public void Role_RefusesReservedAndUnknownPermissions(string permission, string code)
    {
        Assert.Equal(code, Assert.Throws<DomainException>(() => OperationsRole.Create(Tenant, "Clerk", new[] { permission }, Now)).Error.Code);
    }

    [Fact]
    public void Role_NeedsAShortName()
    {
        Assert.Equal("ops.roles.name_required", Assert.Throws<DomainException>(() => OperationsRole.Create(Tenant, " ", Array.Empty<string>(), Now)).Error.Code);
        Assert.Equal("ops.roles.name_too_long", Assert.Throws<DomainException>(() => OperationsRole.Create(Tenant, new string('x', 61), Array.Empty<string>(), Now)).Error.Code);
    }

    [Fact]
    public void AdministratorRole_HoldsEverythingButTheCompanyChange_AndCannotBeChanged()
    {
        OperationsRole administrator = OperationsRole.CreateAdministrator(Tenant, Now);

        Assert.True(administrator.Holds(OperationsPermissions.DevicesApprove));
        Assert.True(administrator.Holds(OperationsPermissions.AuditView));
        Assert.False(administrator.Holds(OperationsPermissions.CompanyRequestChange));
        Assert.Equal("ops.roles.system_role_locked", Assert.Throws<DomainException>(() => administrator.Change("Boss", Array.Empty<string>(), Now)).Error.Code);
        Assert.Equal("ops.roles.system_role_locked", Assert.Throws<DomainException>(() => administrator.SetActive(false, Now)).Error.Code);
    }

    [Fact]
    public void Grant_NeedsAScopeThatMatchesItsKind()
    {
        Guid user = Guid.NewGuid();
        Guid role = Guid.NewGuid();

        Assert.Null(OperationsRoleGrant.Grant(Tenant, user, role, OperationsRoleScope.Tenant, null, null, Now).ScopeId);
        Assert.NotNull(OperationsRoleGrant.Grant(Tenant, user, role, OperationsRoleScope.Warehouse, Guid.NewGuid(), null, Now).ScopeId);
        Assert.Equal("ops.roles.scope_invalid", Assert.Throws<DomainException>(() => OperationsRoleGrant.Grant(Tenant, user, role, OperationsRoleScope.Branch, null, null, Now)).Error.Code);
        Assert.Equal("ops.roles.scope_invalid", Assert.Throws<DomainException>(() => OperationsRoleGrant.Grant(Tenant, user, role, OperationsRoleScope.Tenant, Guid.NewGuid(), null, Now)).Error.Code);
        Assert.Equal("ops.roles.scope_invalid", Assert.Throws<DomainException>(() => OperationsRoleGrant.Grant(Tenant, user, role, OperationsRoleScope.Warehouse, Guid.Empty, null, Now)).Error.Code);
    }

    [Fact]
    public void Grant_IsRevokedOnce()
    {
        OperationsRoleGrant grant = OperationsRoleGrant.Grant(Tenant, Guid.NewGuid(), Guid.NewGuid(), OperationsRoleScope.Tenant, null, null, Now);
        Guid first = Guid.NewGuid();

        grant.Revoke(first, Now);
        grant.Revoke(Guid.NewGuid(), Now.AddHours(1));

        Assert.False(grant.IsActive);
        Assert.Equal(first, grant.RevokedBy);
        Assert.Equal(Now, grant.RevokedUtc);
    }

    [Fact]
    public void Staff_StatusChanges_NeedAReason_AndFollowTheRules()
    {
        OperationsUser clerk = ActiveStaff("ada@northwind.example");

        Assert.Equal("ops.staff.reason_required", Assert.Throws<DomainException>(() => clerk.ChangeStatus(OperationsUserAccountStatus.Suspended, " ", Now)).Error.Code);
        clerk.ChangeStatus(OperationsUserAccountStatus.Suspended, "Investigation", Now);
        Assert.Equal(OperationsUserAccountStatus.Suspended, clerk.Account.Status);
        Assert.Equal("Investigation", clerk.Account.StatusReason);
        Assert.Equal("ops.staff.status_change_invalid", Assert.Throws<DomainException>(() => clerk.ChangeStatus(OperationsUserAccountStatus.Locked, "Again", Now)).Error.Code);

        clerk.ChangeStatus(OperationsUserAccountStatus.Active, "Cleared", Now);
        clerk.ChangeStatus(OperationsUserAccountStatus.Deactivated, "Left the company", Now);
        Assert.Equal(OperationsUserAccountStatus.Deactivated, clerk.Account.Status);
    }

    [Fact]
    public void InvitedStaff_CannotBeReactivatedWithoutAPassword()
    {
        OperationsUser invited = OperationsUser.InviteStaff(Tenant, "Ada Lovelace", EmailAddress.Create("ada@northwind.example"), LanguageCode.En, Now);
        invited.ChangeStatus(OperationsUserAccountStatus.Deactivated, "Never started", Now);

        Assert.Equal("ops.staff.status_change_invalid", Assert.Throws<DomainException>(() => invited.ChangeStatus(OperationsUserAccountStatus.Active, "Back", Now)).Error.Code);
    }

    [Fact]
    public void Owner_IsNeverSuspended()
    {
        OperationsUser owner = OperationsUser.InviteOwner(Tenant, "Grace Hopper", EmailAddress.Create("grace@northwind.example"), LanguageCode.En, Now);
        owner.AcceptInvitation("hash", Now);

        Assert.Equal("ops.staff.owner_protected", Assert.Throws<DomainException>(() => owner.ChangeStatus(OperationsUserAccountStatus.Suspended, "No", Now)).Error.Code);
    }

    [Fact]
    public void Administrator_NeedsASecondFactor_LikeTheOwner()
    {
        OperationsUser clerk = ActiveStaff("ada@northwind.example");
        Assert.False(clerk.RequiresSecondFactor);

        clerk.SetAdministrator(true, Now);

        Assert.True(clerk.RequiresSecondFactor);
    }

    [Fact]
    public void Ownership_GoesOnlyToAnActiveAdministratorWithASecondFactor()
    {
        OperationsUser owner = OperationsUser.InviteOwner(Tenant, "Grace Hopper", EmailAddress.Create("grace@northwind.example"), LanguageCode.En, Now);
        owner.AcceptInvitation("hash", Now);
        OperationsUser next = ActiveStaff("ada@northwind.example");

        Assert.Equal("ops.staff.transfer_needs_administrator", Assert.Throws<DomainException>(() => owner.TransferOwnershipTo(next, Now)).Error.Code);
        next.SetAdministrator(true, Now);
        Assert.Equal("ops.staff.transfer_needs_administrator", Assert.Throws<DomainException>(() => owner.TransferOwnershipTo(next, Now)).Error.Code);

        next.BeginAuthenticatorEnrolment("secret", Now);
        next.ConfirmAuthenticatorApp(100, Now);
        owner.TransferOwnershipTo(next, Now);
        next.BecomeOwner(Now);

        Assert.False(owner.IsOwner);
        Assert.True(owner.IsAdministrator);
        Assert.True(next.IsOwner);
        Assert.False(next.IsAdministrator);
    }

    private static OperationsUser ActiveStaff(string email)
    {
        OperationsUser user = OperationsUser.InviteStaff(Tenant, "Ada Lovelace", EmailAddress.Create(email), LanguageCode.En, Now);
        user.AcceptInvitation("hash", Now);
        return user;
    }
}
