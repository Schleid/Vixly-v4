using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsAccessViewTests.cs
//
// PURPOSE:
// Proves who may do what, and where (Slice 1D).
//
// RESPONSIBILITIES:
// - A grant covers its scope: the company, one branch and its warehouses, or one warehouse
// - A person can give only what they hold, where they hold it
// - Only the Owner gives the Administrator role
// - The role Platform sees: owner, admin or staff
//
// IMPORTANT NOTES:
// - No database
// =========================================================
public sealed class OperationsAccessViewTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid Tenant = Guid.NewGuid();

    private static readonly Guid Miami = Guid.NewGuid();

    private static readonly Guid MiamiWarehouse = Guid.NewGuid();

    private static readonly Guid PortAuPrince = Guid.NewGuid();

    private static readonly Guid PortWarehouse = Guid.NewGuid();

    private static readonly OperationsRole Manager = OperationsRole.Create(Tenant, "Manager", new[] { OperationsPermissions.WarehousesView, OperationsPermissions.WarehousesManage, OperationsPermissions.RolesGrant }, Now);

    private static readonly OperationsRole Viewer = OperationsRole.Create(Tenant, "Viewer", new[] { OperationsPermissions.WarehousesView }, Now);

    [Fact]
    public void BranchGrant_CoversTheBranchAndItsWarehouses_Only()
    {
        OperationsAccessView view = View(Grant(Manager, OperationsRoleScope.Branch, Miami));

        Assert.True(view.Has(OperationsPermissions.WarehousesManage));
        Assert.False(view.HasTenantWide(OperationsPermissions.WarehousesManage));
        Assert.True(view.CoversBranch(OperationsPermissions.WarehousesManage, Miami));
        Assert.True(view.CoversWarehouse(OperationsPermissions.WarehousesManage, MiamiWarehouse, Miami));
        Assert.False(view.CoversBranch(OperationsPermissions.WarehousesManage, PortAuPrince));
        Assert.False(view.CoversWarehouse(OperationsPermissions.WarehousesManage, PortWarehouse, PortAuPrince));
    }

    [Fact]
    public void WarehouseGrant_CoversThatWarehouse_NotItsBranch()
    {
        OperationsAccessView view = View(Grant(Manager, OperationsRoleScope.Warehouse, PortWarehouse));

        Assert.True(view.CoversWarehouse(OperationsPermissions.WarehousesManage, PortWarehouse, PortAuPrince));
        Assert.False(view.CoversBranch(OperationsPermissions.WarehousesManage, PortAuPrince));
        Assert.False(view.CoversWarehouse(OperationsPermissions.WarehousesManage, MiamiWarehouse, Miami));
    }

    [Fact]
    public void RevokedGrantsAndInactiveRoles_GiveNothing()
    {
        OperationsRole paused = OperationsRole.Create(Tenant, "Paused", new[] { OperationsPermissions.WarehousesView }, Now);
        paused.SetActive(false, Now);

        OperationsAccessView view = View(Grant(paused, OperationsRoleScope.Tenant, null));

        Assert.False(view.Has(OperationsPermissions.WarehousesView));
    }

    [Fact]
    public void Granting_IsLimitedToWhatAndWhereTheGranterHolds()
    {
        OperationsAccessView view = View(Grant(Manager, OperationsRoleScope.Branch, Miami));

        Assert.True(view.CanGrant(Viewer, OperationsRoleScope.Branch, Miami, null));
        Assert.True(view.CanGrant(Viewer, OperationsRoleScope.Warehouse, MiamiWarehouse, Miami));
        Assert.False(view.CanGrant(Viewer, OperationsRoleScope.Branch, PortAuPrince, null));
        Assert.False(view.CanGrant(Viewer, OperationsRoleScope.Tenant, null, null));
        Assert.False(view.CanGrant(OperationsRole.Create(Tenant, "Staff boss", new[] { OperationsPermissions.StaffInvite }, Now), OperationsRoleScope.Branch, Miami, null));
        Assert.False(view.CanGrant(OperationsRole.CreateAdministrator(Tenant, Now), OperationsRoleScope.Tenant, null, null));
    }

    [Fact]
    public void Owner_HoldsEverything_AndAloneGivesTheAdministratorRole()
    {
        OperationsUser owner = OperationsUser.InviteOwner(Tenant, "Grace Hopper", EmailAddress.Create("grace@northwind.example"), LanguageCode.En, Now);
        OperationsAccessView view = new(owner, Array.Empty<(OperationsRoleGrant, OperationsRole)>());

        Assert.True(view.HasTenantWide(OperationsPermissions.CompanyRequestChange));
        Assert.True(view.CanGrant(OperationsRole.CreateAdministrator(Tenant, Now), OperationsRoleScope.Tenant, null, null));
        Assert.Equal("owner", view.PlatformRole);
    }

    [Fact]
    public void PlatformRole_FollowsTheAdministratorFlag()
    {
        OperationsUser staff = OperationsUser.InviteStaff(Tenant, "Ada Lovelace", EmailAddress.Create("ada@northwind.example"), LanguageCode.En, Now);
        Assert.Equal("staff", new OperationsAccessView(staff, Array.Empty<(OperationsRoleGrant, OperationsRole)>()).PlatformRole);

        staff.SetAdministrator(true, Now);

        Assert.Equal("admin", new OperationsAccessView(staff, Array.Empty<(OperationsRoleGrant, OperationsRole)>()).PlatformRole);
    }

    private static (OperationsRoleGrant, OperationsRole) Grant(OperationsRole role, OperationsRoleScope scope, Guid? scopeId) =>
        (OperationsRoleGrant.Grant(Tenant, Guid.NewGuid(), role.Id, scope, scopeId, null, Now), role);

    private static OperationsAccessView View(params (OperationsRoleGrant, OperationsRole)[] grants)
    {
        OperationsUser staff = OperationsUser.InviteStaff(Tenant, "Ada Lovelace", EmailAddress.Create("ada@northwind.example"), LanguageCode.En, Now);
        return new OperationsAccessView(staff, grants);
    }
}
