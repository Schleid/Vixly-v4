using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tests.Fakes;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Audit;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsCustomerLookupTests.cs
//
// PURPOSE:
// Proves the customer lookup of the branches and roles design (R-4): find one customer, record it, limit it.
//
// RESPONSIBILITIES:
// - Every lookup is recorded without what was typed
// - A person with only the lookup is stopped after OperationsAccessLimits.CustomerLookupsPerHour lookups in an hour
// - A person with the company's customer list is never counted
//
// IMPORTANT NOTES:
// - In-memory stores; the audit reader is a small counting fake
// =========================================================
public sealed class OperationsCustomerLookupTests
{
    private readonly IdentityTestKit _kit = new();

    private readonly CountingAuditReader _reader = new();

    [Fact]
    public async Task Lookup_IsRecorded_WithoutWhatWasTyped()
    {
        (IReadOnlyList<Vixly.Operations.Domain.Customers.OperationsCustomer> found, Vixly.Shared.Kernel.Errors.Error? error) =
            await Lookups().FindAsync(Desk(), " jean@example.com ", 5, CancellationToken.None);

        Assert.Null(error);
        Assert.Empty(found);
        OperationsAuditEntry entry = Assert.Single(_kit.Store.Audit);
        Assert.Equal(OperationsCustomerLookups.ActionCode, entry.ActionCode);
        Assert.DoesNotContain("jean", entry.AfterJson ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("lookups", _reader.LastFilter!.Area);
        Assert.Equal(_kit.Clock.GetUtcNow().AddHours(-1), _reader.LastFilter.FromUtc);
    }

    [Fact]
    public async Task PersonWithOnlyTheLookup_IsStopped_AtTheHourlyLimit()
    {
        _reader.Count = OperationsAccessLimits.CustomerLookupsPerHour;

        (_, Vixly.Shared.Kernel.Errors.Error? error) = await Lookups().FindAsync(Desk(), "C-1001", 5, CancellationToken.None);

        Assert.Equal("ops.customers.lookup_limit", error!.Code);
        Assert.Empty(_kit.Store.Audit);
    }

    [Fact]
    public async Task PersonWithTheCustomerList_IsNeverCounted()
    {
        _reader.Count = OperationsAccessLimits.CustomerLookupsPerHour * 10;

        (_, Vixly.Shared.Kernel.Errors.Error? error) = await Lookups().FindAsync(View(OperationsPermissions.CustomersView), "C-1001", 5, CancellationToken.None);

        Assert.Null(error);
        Assert.Null(_reader.LastFilter);
        Assert.Single(_kit.Store.Audit);
    }

    [Fact]
    public async Task EmptyText_FindsNothing_AndRecordsNothing()
    {
        (_, Vixly.Shared.Kernel.Errors.Error? error) = await Lookups().FindAsync(Desk(), "   ", 5, CancellationToken.None);

        Assert.Null(error);
        Assert.Empty(_kit.Store.Audit);
    }

    private OperationsCustomerLookups Lookups() => new(new FakeCustomerStore(), _reader, _kit.Audit, _kit.Store, _kit.Clock);

    private OperationsAccessView Desk() => View(OperationsPermissions.CustomersLookup);

    private OperationsAccessView View(string permission)
    {
        DateTimeOffset now = _kit.Clock.GetUtcNow();
        OperationsUser staff = OperationsUser.InviteStaff(_kit.TenantId, "Marie Desk", EmailAddress.Create("marie@northwind.example"), LanguageCode.En, now);
        OperationsRole role = OperationsRole.Create(_kit.TenantId, "Desk", new[] { permission }, now);
        return new OperationsAccessView(staff, new[] { (OperationsRoleGrant.Grant(_kit.TenantId, staff.Id, role.Id, OperationsRoleScope.Tenant, null, null, now), role) });
    }

    private sealed class CountingAuditReader : IOperationsAuditReader
    {
        public int Count { get; set; }

        public OperationsAuditFilter? LastFilter { get; private set; }

        public Task<(IReadOnlyList<OperationsAuditEntry> Items, int TotalCount)> ListAsync(OperationsAuditFilter filter, int page, int pageSize, CancellationToken cancellationToken)
        {
            LastFilter = filter;
            return Task.FromResult<(IReadOnlyList<OperationsAuditEntry>, int)>((Array.Empty<OperationsAuditEntry>(), Count));
        }
    }
}
