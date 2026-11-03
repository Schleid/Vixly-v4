using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tests.Fakes;
using Vixly.Operations.Domain.Access;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsStandardRoleTests.cs
//
// PURPOSE:
// Proves every company gets the standard roles (owner, 2026-10-29: branches and roles design, section 5, R-5).
//
// RESPONSIBILITIES:
// - The ten roles (the support agent added 2026-10-29, section 9), named in the company's language, each with its permissions
// - Never twice, never over a role the company renamed, never over a name the company uses
// - Security tasks are in the Security administrator role; a role given for a branch or a warehouse holds no company-only permission of use
//
// IMPORTANT NOTES:
// - The names come from a fake that tags the language, so the test sees which language was asked
// =========================================================
public sealed class OperationsStandardRoleTests
{
    private static readonly Guid Tenant = Guid.NewGuid();

    private readonly FakeRoleStore _roles = new();

    private readonly FakePlatformClient _platform = new();

    private readonly FixedTimeProvider _clock = new(new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task EveryCompany_GetsTheTenRoles_InItsLanguage_Once()
    {
        _platform.Accounts[Tenant] = new OperationsTenantAccount(Tenant, "galeo", "Galeo Logistics LLC", "Galeo Logistics", "fr", "Owner", null);

        Assert.Equal(10, await Setup().EnsureAsync(Tenant, CancellationToken.None));
        Assert.Equal(0, await Setup().EnsureAsync(Tenant, CancellationToken.None));

        Assert.Equal(OperationsStandardRoles.All.Select(template => template.Key), _roles.Roles.Select(role => role.TemplateKey));
        Assert.All(_roles.Roles, role => Assert.StartsWith("fr:", role.Name, StringComparison.Ordinal));
        OperationsRole security = _roles.Roles.Single(role => role.TemplateKey == "security_admin");
        Assert.True(security.Holds(OperationsPermissions.DevicesApprove) && security.Holds(OperationsPermissions.StaffInvite) && !security.Holds(OperationsPermissions.PaymentsTake));
        OperationsRole cashier = _roles.Roles.Single(role => role.TemplateKey == "cashier");
        Assert.True(cashier.Holds(OperationsPermissions.PaymentsTake) && !cashier.Holds(OperationsPermissions.PaymentsRefund));
        Assert.True(_roles.Roles.Single(role => role.TemplateKey == "branch_manager").Holds(OperationsPermissions.DiscountsApprove));
        Assert.False(_roles.Roles.Single(role => role.TemplateKey == "warehouse_manager").Holds(OperationsPermissions.DiscountsApprove));
        Assert.Equal(new[] { OperationsPermissions.SupportLookup }, _roles.Roles.Single(role => role.TemplateKey == "support_agent").Permissions);
    }

    [Fact]
    public async Task ARenamedOrDeactivatedRole_IsKept_AndATakenNameGetsANumber()
    {
        _roles.Add(OperationsRole.Create(Tenant, "en:cashier", new[] { OperationsPermissions.CounterServe }, _clock.GetUtcNow()));
        await Setup().EnsureAsync(Tenant, CancellationToken.None);

        OperationsRole cashier = _roles.Roles.Single(role => role.TemplateKey == "cashier");
        Assert.Equal("en:cashier 2", cashier.Name);

        cashier.Change("Caisse principale", new[] { OperationsPermissions.PaymentsTake }, _clock.GetUtcNow());
        _roles.Roles.Single(role => role.TemplateKey == "driver").SetActive(false, _clock.GetUtcNow());
        Assert.Equal(0, await Setup().EnsureAsync(Tenant, CancellationToken.None));
        Assert.Equal(11, _roles.Roles.Count);
    }

    [Fact]
    public async Task WithoutPlatform_TheNamesAreInEnglish()
    {
        _platform.Down = true;

        await Setup().EnsureAsync(Tenant, CancellationToken.None);

        Assert.All(_roles.Roles, role => Assert.StartsWith("en:", role.Name, StringComparison.Ordinal));
    }

    [Fact]
    public void EveryStandardRole_HoldsOnlyKnownPermissions_AndNoReservedOne()
    {
        foreach ((string key, IReadOnlyList<string> permissions) in OperationsStandardRoles.All)
        {
            Assert.True(key.Length <= OperationsStandardRoles.KeyMaxLength, key);
            Assert.All(permissions, permission => Assert.True(OperationsPermissions.IsKnown(permission), key + " " + permission));
            Assert.DoesNotContain(permissions, permission => OperationsPermissions.SystemOnly.Contains(permission));
        }
    }

    private OperationsStandardRoleSetup Setup() => new(_roles, new TaggedNames(), _platform, _clock);

    private sealed class TaggedNames : IOperationsRoleNames
    {
        public string Name(string key, string language) => language + ":" + key;
    }
}
