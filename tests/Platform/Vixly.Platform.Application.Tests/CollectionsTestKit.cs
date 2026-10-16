using Vixly.Platform.Application.Collections;
using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Application.UseCases.Collections;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: CollectionsTestKit.cs
//
// PURPOSE:
// Builds the collections engine over the payments, billing and subscription fakes, with helpers to move days.
//
// RESPONSIBILITIES:
// - Create an active tenant with a paid subscription and an invoice due today
// - Run the clock on a given day of the timeline
// - Deliver collections emails through the outbox handler
//
// IMPORTANT NOTES:
// - Day N means N days after the invoice's due date, at 06:00 UTC
// =========================================================
internal sealed class CollectionsTestKit
{
    public CollectionsTestKit()
    {
        Payments.Kit.Collections.Invoices = () => Payments.Billing.Invoices;
    }

    public PaymentsTestKit Payments { get; } = new();

    public SubscriptionTestKit Kit => Payments.Kit;

    public FakeCollectionsStore Store => Kit.Collections;

    public PlatformCollectionsAdministration Administration()
    {
        return new PlatformCollectionsAdministration(
            Payments.BillingAdministration(),
            Kit.Subscriptions(),
            Payments.Administration(),
            Store,
            Store,
            Kit.Tenants.Tenants,
            Kit.Tenants.Audit(),
            Kit.Tenants.Store,
            Payments.Zelle);
    }

    public (PlatformTenant Tenant, PlatformSubscription Subscription, PlatformInvoice Invoice) Overdue(decimal amount = 120m)
    {
        PlatformTenant tenant = Kit.Tenants.Tenant("atlas-" + Guid.NewGuid().ToString("N")[..6], active: true);
        PlatformSubscription subscription = Kit.Subscribe(tenant.Id, Kit.Plan());
        PlatformInvoice invoice = PlatformInvoice.Issue(
            tenant.Id, subscription.Id, PlatformInvoiceKind.Renewal, "INV-2026-" + (Payments.Billing.Invoices.Count + 1).ToString("D6"), "src-" + Guid.NewGuid().ToString("N"), "USD", LanguageCode.Default,
            PlatformInvoiceParty.Create("Atlas Freight LLC", "1 Dock Road", null, "billing@atlas.test"),
            0, null, null,
            new[] { new PlatformInvoiceLineInput(PlatformInvoiceLineKind.PlanPeriod, "ui.invoice_line.plan_period", new Dictionary<string, string>(StringComparer.Ordinal), 1m, amount, 0m, null) },
            Kit.Now).Invoice;
        Payments.Billing.Invoices.Add(invoice);
        Payments.Billing.Profile ??= PlatformCompanyProfile.Create(
            PlatformCompanyDetails.Create("Vixly Inc.", null, EmailAddress.Create("billing@vixly.example"), PhoneNumber.Create("+1 305 555 0100"),
                PlatformPostalAddress.Create("1 Main St", null, "Miami", "FL", "33132", "US")),
            null, null, Guid.NewGuid(), Kit.Now);
        return (tenant, subscription, invoice);
    }

    public PlatformInvoice FirstPeriodInvoice(Guid tenantId, Guid subscriptionId)
    {
        PlatformInvoice invoice = PlatformInvoice.Issue(
            tenantId, subscriptionId, PlatformInvoiceKind.FirstPeriod, "INV-2026-" + (Payments.Billing.Invoices.Count + 1).ToString("D6"), "src-" + Guid.NewGuid().ToString("N"), "USD", LanguageCode.Default,
            PlatformInvoiceParty.Create("Orbit Freight LLC", "1 Dock Road", null, "billing@orbit.test"),
            0, null, null,
            new[] { new PlatformInvoiceLineInput(PlatformInvoiceLineKind.PlanPeriod, "ui.invoice_line.plan_period", new Dictionary<string, string>(StringComparer.Ordinal), 1m, 30m, 0m, null) },
            Kit.Now).Invoice;
        Payments.Billing.Invoices.Add(invoice);
        return invoice;
    }

    public void Day(int day, PlatformInvoice invoice)
    {
        DateTimeOffset target = new(invoice.DueUtc.UtcDateTime.Date.AddDays(day).AddHours(6), TimeSpan.Zero);
        Kit.Tenants.Clock.Advance(target - Kit.Now);
    }

    public Task<int> RunAsync() => new PlatformCollectionsClock(Administration()).RunAsync(CancellationToken.None);

    public IReadOnlyList<PlatformCollectionsNotice> Notices()
    {
        return Kit.Tenants.Tenants.Outbox
            .Where(item => item.Type == PlatformMessageTypes.CollectionsNotice)
            .Select(PlatformOutboxWriter.Read<PlatformCollectionsNotice>)
            .ToArray();
    }

    public async Task DeliverAsync()
    {
        PlatformCollectionsNoticeHandler handler = new(Administration(), Store, Payments.ZelleAdministration());

        foreach (PlatformOutboxMessage message in Kit.Tenants.Tenants.Outbox.Where(item => item.Type == PlatformMessageTypes.CollectionsNotice && item.ProcessedUtc is null).ToArray())
        {
            await handler.HandleAsync(message, CancellationToken.None);
            message.MarkProcessed(Kit.Now);
        }
    }

    public Task PayAsync(PlatformInvoice invoice)
    {
        return new PlatformPaymentApplier(Payments.BillingAdministration(), Kit.Subscriptions())
            .ApplyAsync(null, invoice, invoice.Balance, PlatformPaymentMethod.Cheque, Kit.Now, "CHQ-1001", null, null, CancellationToken.None);
    }
}
