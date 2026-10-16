using System.Text.Json;
using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PaymentsTestKit.cs
//
// PURPOSE:
// Builds the Slice 5 services over in-memory stores and a fake card provider.
//
// RESPONSIBILITIES:
// - Give tests a ready merchant account, a tenant with an open invoice, and the services
// - Deliver provider events through the real webhook use case and handler
//
// IMPORTANT NOTES:
// - Shares the clock, audit and outbox of SubscriptionTestKit
// =========================================================
internal sealed class PaymentsTestKit
{
    public PaymentsTestKit()
    {
        Payments = new FakePaymentsStore(Billing);
    }

    public SubscriptionTestKit Kit { get; } = new();

    public FakeBillingStore Billing { get; } = new();

    public FakePaymentsStore Payments { get; }

    public FakeCardGateway Gateway { get; } = new();

    public FakeZelleStore Zelle { get; } = new();

    public PlatformZelleAdministration ZelleAdministration()
    {
        return new PlatformZelleAdministration(Administration(), Kit.Subscriptions(), Zelle, Zelle, Kit.Tenants.Outbox());
    }

    public DateTimeOffset Now => Kit.Now;

    public PlatformBillingAdministration BillingAdministration()
    {
        return new PlatformBillingAdministration(
            Kit.Tenants.Gate(),
            new PlatformStepUpGuard(Kit.Tenants.Store, Kit.Tenants.Clock),
            Kit.Tenants.Store,
            Billing,
            Billing,
            Billing,
            Kit.Tenants.Audit(),
            Kit.Tenants.Outbox(),
            Kit.Tenants.Clock);
    }

    public PlatformPaymentsAdministration Administration()
    {
        return new PlatformPaymentsAdministration(BillingAdministration(), Payments, Gateway, Kit.Tenants.Tenants, Kit.Tenants.Crypto, Kit.Sales, Kit.Tenants.Audit(), Kit.Tenants.Store);
    }

    public PlatformCardSettlement Settlement(PlatformPaymentsAdministration payments)
    {
        return new PlatformCardSettlement(payments, new PlatformPaymentApplier(payments.Billing, Kit.Subscriptions()));
    }

    // A ready Stripe test account accepting USD.
    public void Ready()
    {
        PlatformMerchantAccount account = PlatformMerchantAccount.Create(PlatformCardProvider.Stripe, Now);
        account.Update(PlatformCardProvider.Stripe, PlatformPaymentMode.Test, "pk_test_abc", new[] { "USD" }, true, Guid.NewGuid(), Now);
        account.RecordCheck(true, null, Now);
        Payments.Merchant = account;
    }

    public (PlatformTenant Tenant, PlatformInvoice Invoice) OpenInvoice(decimal price = 100m, PlatformInvoiceKind kind = PlatformInvoiceKind.Renewal)
    {
        PlatformTenant tenant = Kit.Tenants.Tenant("atlas-" + Guid.NewGuid().ToString("N")[..6]);
        PlatformInvoice invoice = PlatformInvoice.Issue(
            tenant.Id, null, kind, "INV-2026-" + (Billing.Invoices.Count + 1).ToString("D6"), "src-" + Guid.NewGuid().ToString("N"), "USD", LanguageCode.Default,
            PlatformInvoiceParty.Create("Atlas Freight LLC", "1 Dock Road", null, "billing@atlas.test"),
            0, Now, Now.AddMonths(1),
            new[] { new PlatformInvoiceLineInput(PlatformInvoiceLineKind.PlanPeriod, "ui.invoice_line.plan_period", new Dictionary<string, string>(StringComparer.Ordinal), 1m, price, 0m, null) },
            Now).Invoice;
        Billing.Invoices.Add(invoice);
        Billing.Profile = PlatformCompanyProfile.Create(
            PlatformCompanyDetails.Create("Vixly Inc.", null, EmailAddress.Create("billing@vixly.example"), PhoneNumber.Create("+1 305 555 0100"),
                PlatformPostalAddress.Create("1 Main St", null, "Miami", "FL", "33132", "US")),
            null, null, Guid.NewGuid(), Now);
        return (tenant, invoice);
    }

    public PlatformSavedCard SaveCard(Guid tenantId, bool autoCharge = false, int expiryYear = 2030)
    {
        PlatformTenantCardProfile profile = PlatformTenantCardProfile.Create(tenantId, PlatformCardProvider.Stripe, PlatformPaymentMode.Test, "cus_x", Now);

        if (autoCharge)
        {
            profile.GiveConsent("owner@atlas.test", PlatformPaymentsAdministration.ConsentTextVersion, Now);
        }

        Payments.Profiles.Add(profile);
        PlatformSavedCard card = PlatformSavedCard.Create(tenantId, PlatformCardProvider.Stripe, "pm_" + Guid.NewGuid().ToString("N")[..8], "visa", "4242", 12, expiryYear, PlatformCardAddedVia.SaveCardLink, Now);
        Payments.Cards.Add(card);
        return card;
    }

    public static string Event(PlatformCardEventKind kind, string eventId, Guid? transactionId = null, string? paymentId = null, string? refundId = null, Dictionary<string, string>? metadata = null, string? paymentMethodId = null, bool? won = null, string? failure = null)
    {
        Dictionary<string, string> data = metadata ?? new Dictionary<string, string>(StringComparer.Ordinal);

        if (transactionId is Guid id)
        {
            data[PlatformCardMetadata.Transaction] = id.ToString("D");
        }

        return JsonSerializer.Serialize(new PlatformCardEvent(eventId, kind.ToString(), kind, paymentId, null, paymentId, refundId, paymentMethodId, "cus_x", failure, won, data, DateTimeOffset.UnixEpoch), FakeCardGateway.Json);
    }

    // Through the webhook use case, then the outbox handler, as in production.
    public async Task<bool> DeliverAsync(string payload)
    {
        PlatformPaymentsAdministration payments = Administration();
        PlatformOutboxWriter outbox = Kit.Tenants.Outbox();
        int before = Kit.Tenants.Tenants.Outbox.Count;
        var response = await new Vixly.Platform.Application.UseCases.Payments.ReceivePlatformProviderEvent.ReceivePlatformProviderEventUseCase(payments, outbox).ExecuteAsync(
            PlatformAnonymousAuthorizationContext.Instance,
            new Vixly.Platform.Application.UseCases.Payments.ReceivePlatformProviderEvent.ReceivePlatformProviderEventRequest { Payload = payload, Signature = "valid" },
            CancellationToken.None);

        if (!response.Succeeded || response.Duplicate || Kit.Tenants.Tenants.Outbox.Count == before)
        {
            return false;
        }

        PlatformOutboxMessage message = Kit.Tenants.Tenants.Outbox.Last(item => item.Type == PlatformMessageTypes.ProviderEvent);
        await new PlatformProviderEventHandler(payments, Settlement(payments)).HandleAsync(message, CancellationToken.None);
        return true;
    }
}
