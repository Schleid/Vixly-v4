using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tests.Fakes;
using Vixly.Operations.Application.UseCases.Locations.ListOperationsLocations;
using Vixly.Operations.Application.UseCases.Locations.SaveOperationsWarehouse;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsWarehouseScopeTests.cs
//
// PURPOSE:
// The two-warehouse matrix: a user limited to warehouse A never reads or changes warehouse B (Slice 1D, design section 11).
//
// RESPONSIBILITIES:
// - Listing shows only the warehouses and branches in scope
// - Saving a warehouse out of scope, or moving one out of scope, is refused
// - A role without the permission sees nothing
//
// IMPORTANT NOTES:
// - Use cases with in-memory stores
// =========================================================
public sealed class OperationsWarehouseScopeTests
{
    private readonly IdentityTestKit _kit = new();

    private readonly FakeSiteStore _site = new();

    private readonly FakeRoleStore _roles = new();

    private readonly OperationsBranch _miami;

    private readonly OperationsBranch _haiti;

    private readonly OperationsWarehouse _a;

    private readonly OperationsWarehouse _b;

    public OperationsWarehouseScopeTests()
    {
        DateTimeOffset now = _kit.Clock.GetUtcNow();
        _miami = OperationsBranch.Create(_kit.TenantId, Details("MIA", "US"), OperationsBranchSide.Origin, now);
        _haiti = OperationsBranch.Create(_kit.TenantId, Details("PAP", "HT"), OperationsBranchSide.Destination, now);
        _a = OperationsWarehouse.Create(_kit.TenantId, _miami.Id, Details("WHA", "US"), OperationsWarehouseRoles.Receiving, true, now);
        _b = OperationsWarehouse.Create(_kit.TenantId, _haiti.Id, Details("WHB", "HT"), OperationsWarehouseRoles.PickupPoint, true, now);
        _site.Add(_miami);
        _site.Add(_haiti);
        _site.Add(_a);
        _site.Add(_b);
    }

    [Fact]
    public async Task WarehouseScopedUser_SeesOnlyWarehouseA()
    {
        OperationsAuthorizationContext clerk = Staff(OperationsRoleScope.Warehouse, _a.Id, OperationsPermissions.WarehousesView, OperationsPermissions.WarehousesManage);

        ListOperationsLocationsResponse list = await List(clerk);

        Assert.True(list.Succeeded);
        Assert.Equal(new[] { _a.Id }, list.Warehouses.Select(warehouse => warehouse.Id));
        Assert.Empty(list.Branches);
        Assert.Empty(list.Contacts);
    }

    [Fact]
    public async Task BranchScopedUser_SeesTheBranchAndItsWarehouses()
    {
        OperationsAuthorizationContext manager = Staff(OperationsRoleScope.Branch, _haiti.Id, OperationsPermissions.BranchesView, OperationsPermissions.WarehousesView);

        ListOperationsLocationsResponse list = await List(manager);

        Assert.Equal(new[] { _haiti.Id }, list.Branches.Select(branch => branch.Id));
        Assert.Equal(new[] { _b.Id }, list.Warehouses.Select(warehouse => warehouse.Id));
    }

    [Fact]
    public async Task WarehouseScopedUser_CannotChangeOrMoveIntoWarehouseB()
    {
        OperationsAuthorizationContext clerk = Staff(OperationsRoleScope.Warehouse, _a.Id, OperationsPermissions.WarehousesView, OperationsPermissions.WarehousesManage);

        Assert.True((await Save(clerk, _a.Id, _miami.Id, "WHA")).Succeeded);
        Assert.Equal("ops.access.permission_required", (await Save(clerk, _b.Id, _haiti.Id, "WHB")).Error?.Code);
        Assert.Equal("ops.access.permission_required", (await Save(clerk, _a.Id, _haiti.Id, "WHA")).Error?.Code);
        Assert.Equal("ops.access.permission_required", (await Save(clerk, null, _miami.Id, "WHC")).Error?.Code);
        Assert.Equal(_haiti.Id, _b.BranchId);
    }

    [Fact]
    public async Task ScopedGrant_GivesNoCompanyPowers()
    {
        OperationsAuthorizationContext lead = Staff(OperationsRoleScope.Warehouse, _a.Id, OperationsPermissions.StaffView, OperationsPermissions.StaffSuspend, OperationsPermissions.RolesGrant, OperationsPermissions.WarehousesView);
        OperationsAccess access = new(_kit.Store, _roles, _kit.Store, _kit.Clock, _site, new FakePlanFeatures());

        foreach (string permission in new[] { OperationsPermissions.StaffView, OperationsPermissions.StaffSuspend, OperationsPermissions.RolesGrant })
        {
            Assert.Equal("ops.access.permission_required", (await access.RequireAsync(lead, permission, false, CancellationToken.None)).Refused?.Code);
        }

        Assert.True((await List(lead)).Succeeded);
    }

    [Fact]
    public async Task RoleWithoutThePermission_SeesNothing()
    {
        OperationsAuthorizationContext editor = Staff(OperationsRoleScope.Tenant, null, OperationsPermissions.BrandEdit);

        Assert.Equal("ops.access.permission_required", (await List(editor)).Error?.Code);
    }

    private OperationsAuthorizationContext Staff(OperationsRoleScope scope, Guid? scopeId, params string[] permissions)
    {
        DateTimeOffset now = _kit.Clock.GetUtcNow();
        OperationsUser user = OperationsUser.InviteStaff(_kit.TenantId, "Ada Lovelace", EmailAddress.Create(Guid.NewGuid().ToString("N")[..8] + "@northwind.example"), LanguageCode.En, now);
        user.AcceptInvitation("hash", now);
        _kit.Store.Users.Add(user);
        OperationsRole role = OperationsRole.Create(_kit.TenantId, "Role " + Guid.NewGuid().ToString("N")[..6], permissions, now);
        _roles.Add(role);
        _roles.Add(OperationsRoleGrant.Grant(_kit.TenantId, user.Id, role.Id, scope, scopeId, null, now));
        return _kit.Context.WithUser(user.Id, Guid.NewGuid());
    }

    private OperationsOwnerCheck Check() => new(_kit.Store, new OperationsAccess(_kit.Store, _roles, _kit.Store, _kit.Clock, _site, new FakePlanFeatures()));

    private Task<ListOperationsLocationsResponse> List(OperationsAuthorizationContext context) =>
        new ListOperationsLocationsUseCase(_site, Check(), _kit.Store).ExecuteAsync(context, new ListOperationsLocationsRequest(), CancellationToken.None);

    private Task<SaveOperationsWarehouseResponse> Save(OperationsAuthorizationContext context, Guid? warehouseId, Guid branchId, string code) =>
        new SaveOperationsWarehouseUseCase(_kit.Store, _site, Check(), _kit.Audit, _kit.Store, _kit.Clock).ExecuteAsync(context, new SaveOperationsWarehouseRequest
        {
            WarehouseId = warehouseId, BranchId = branchId, Code = code, Name = "Warehouse " + code, Line1 = "1 Harbor Road", City = "Miami", CountryCode = "US",
            Roles = OperationsWarehouseRoles.Receiving, AcceptingDeliveries = true
        }, CancellationToken.None);

    private static OperationsLocationDetails Details(string code, string country) =>
        new(code, "Location " + code, OperationsPostalAddress.Create("1 Main Street", null, "City", null, null, country), null, null, null, true);
}
