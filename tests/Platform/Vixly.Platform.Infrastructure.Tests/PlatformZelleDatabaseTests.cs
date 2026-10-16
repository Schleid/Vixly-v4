using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;
using Vixly.Platform.Infrastructure.Persistence;
using Vixly.Platform.Infrastructure.Persistence.Queries;
using Vixly.Platform.Infrastructure.Persistence.Repositories;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: PlatformZelleDatabaseTests.cs
//
// PURPOSE:
// Proves the Zelle mapping against SQL Server (Slice 5 update).
//
// RESPONSIBILITIES:
// - Round-trip the Zelle details with the QR image, and a report with its decision
// - The details read model never carries the image; the QR query does
// - One waiting report per invoice, even from another host
//
// IMPORTANT NOTES:
// - Needs VIXLY_TEST_SQL_SERVER; skipped otherwise
// =========================================================
public sealed class PlatformZelleDatabaseTests : IClassFixture<PlatformDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4 };

    private readonly PlatformDatabaseFixture _database;

    public PlatformZelleDatabaseTests(PlatformDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task DetailsAndReport_RoundTrip_AndOnlyOneReportWaitsPerInvoice()
    {
        Guid actor = _database.ActorUserId == Guid.Empty ? Guid.NewGuid() : _database.ActorUserId;
        (PlatformTenant tenant, PlatformInvoice invoice) = NewInvoice(actor);
        PlatformZelleClaim claim = PlatformZelleClaim.Submit(tenant.Id, invoice.Id, invoice.Number, "USD", invoice.Balance, 50m, Now.UtcDateTime.Date, "Grace Hopper", "ZL-DB-1", PlatformZelleClaimSource.PaymentLink, Now);

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.Tenants.Add(tenant);
            context.Invoices.Add(invoice);
            PlatformZelleRepository repository = new(context);

            if (await repository.GetAccountAsync(CancellationToken.None) is not { } account)
            {
                account = PlatformZelleAccount.Create(Now);
                repository.AddAccount(account);
            }

            account.Update(true, "Vixly Inc.", "pay@vixly.test", "VixlyPay", "+1 305 555 0100", actor, Now);
            account.SetQrCode(Png, "image/png", actor, Now);
            repository.AddClaim(claim);
            Assert.True(await new PlatformUnitOfWork(context).SaveChangesAsync(CancellationToken.None));
        }

        await using (PlatformDbContext other = _database.NewContext())
        {
            other.ZelleClaims.Add(PlatformZelleClaim.Submit(tenant.Id, invoice.Id, invoice.Number, "USD", invoice.Balance, 50m, Now.UtcDateTime.Date, "Grace", "ZL-DB-2", PlatformZelleClaimSource.PaymentLink, Now));
            Assert.False(await new PlatformUnitOfWork(other).SaveChangesAsync(CancellationToken.None));
        }

        await using PlatformDbContext check = _database.NewContext();
        PlatformZelleRepository store = new(check);
        PlatformZelleQueries queries = new(check);

        PlatformZelleAccountItem details = (await queries.GetAccountAsync(CancellationToken.None))!;
        Assert.True(details.Enabled);
        Assert.True(details.HasQrCode);
        Assert.Equal("VixlyPay", details.Tag);
        PlatformZelleImage image = (await queries.GetQrCodeAsync(CancellationToken.None))!;
        Assert.Equal(Png, image.Content);
        Assert.Equal("image/png", image.ContentType);

        Assert.Equal(claim.Id, (await store.FindWaitingAsync(invoice.Id, CancellationToken.None))!.Id);
        Assert.Equal(1, await store.CountForInvoiceAsync(invoice.Id, CancellationToken.None));
        Assert.Equal(new[] { invoice.Id }, await store.ListWaitingInvoiceIdsAsync(tenant.Id, CancellationToken.None));

        PlatformZelleClaimItem row = Assert.Single((await queries.ListClaimsAsync(new PlatformZelleClaimFilter(PlatformZelleClaimStatus.Waiting, tenant.Id, "ZL-DB"), PagedRequest.Create(1, 25), CancellationToken.None)).Items);
        Assert.Equal(invoice.Balance, row.InvoiceBalance);
        Assert.Equal(tenant.Company.TradingName, row.TenantName);
        Assert.Equal(Now.UtcDateTime.Date, row.SentOn);

        PlatformZelleClaim loaded = (await store.GetClaimAsync(claim.Id, CancellationToken.None))!;
        loaded.Reject("No payment with this code reached us", "Checked the bank", actor, Now);
        Assert.True(await new PlatformUnitOfWork(check).SaveChangesAsync(CancellationToken.None));
        Assert.Empty(await store.ListWaitingInvoiceIdsAsync(tenant.Id, CancellationToken.None));
        Assert.Equal("No payment with this code reached us", (await queries.GetClaimAsync(claim.Id, CancellationToken.None))!.RejectReason);
    }

    private static (PlatformTenant Tenant, PlatformInvoice Invoice) NewInvoice(Guid actor)
    {
        PlatformTenant tenant = PlatformTenant.CreateByStaff(
            "TEN-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(),
            "zel-" + Guid.NewGuid().ToString("N")[..8],
            PlatformCompanyDetails.Create("Orbit Freight LLC", "Orbit " + Guid.NewGuid().ToString("N")[..4], EmailAddress.Create("office@orbit.example"), PhoneNumber.Create("+1 305 555 0199"),
                PlatformPostalAddress.Create("1 Dock Road", null, "Miami", "FL", "33101", "US")),
            PlatformTenantContactDetails.Create(PlatformTenantContactRole.Owner, "Ada Byron", EmailAddress.Create("owner@orbit.example"), null),
            LanguageCode.En,
            "America/New_York",
            actor,
            Now).Tenant;
        PlatformInvoice invoice = PlatformInvoice.Issue(tenant.Id, null, PlatformInvoiceKind.Renewal, "INV-Z-" + Guid.NewGuid().ToString("N")[..10], "test:" + Guid.NewGuid().ToString("N"), "USD", LanguageCode.En,
            PlatformInvoiceParty.Create("Orbit Freight LLC", "1 Dock Road", null, "billing@orbit.example"), 0, Now, Now.AddMonths(1),
            new[] { new PlatformInvoiceLineInput(PlatformInvoiceLineKind.PlanPeriod, "ui.invoice_line.charge", new Dictionary<string, string>(StringComparer.Ordinal), 1m, 99m, 0m, null) },
            Now).Invoice;
        return (tenant, invoice);
    }
}
