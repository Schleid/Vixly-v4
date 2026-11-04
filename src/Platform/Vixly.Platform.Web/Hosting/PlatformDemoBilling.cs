using System.Globalization;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Paging;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Web.Hosting;

// =========================================================
// FILE: PlatformDemoBilling.cs
//
// PURPOSE:
// Demo data for Slice 4: a company profile, tax rates and invoices for the demo subscriptions.
//
// RESPONSIBILITIES:
// - Add the issuer profile and a few tax rates (Canada and Quebec, Spain, France)
// - Invoice the current period of each paying demo subscription
// - Pay the first invoices in full and one in part, with receipts
//
// IMPORTANT NOTES:
// - Development only; off unless PlatformDemoData:SeedOnStartup is true
// - Safe to run again: the profile and rates are added once, invoices are keyed on the subscription period
// - Invoices go through the same issuer as the real handlers (numbers, tax, stored PDFs)
// =========================================================
public sealed class PlatformDemoBilling
{
    private readonly PlatformBillingAdministration _billing;

    private readonly PlatformInvoiceIssuer _issuer;

    private readonly IPlatformTenantQueries _tenants;

    private readonly IPlatformTenantRepository _tenantRecords;

    private readonly IPlatformSubscriptionRepository _subscriptions;

    private readonly IPlatformPlanRepository _plans;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public PlatformDemoBilling(
        PlatformBillingAdministration billing,
        IPlatformTenantQueries tenants,
        IPlatformTenantRepository tenantRecords,
        IPlatformSubscriptionRepository subscriptions,
        IPlatformPlanRepository plans,
        IPlatformUnitOfWork unitOfWork)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _issuer = new PlatformInvoiceIssuer(billing);
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _tenantRecords = tenantRecords ?? throw new ArgumentNullException(nameof(tenantRecords));
        _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<string> SeedAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<PlatformStaffOptionItem> staff = await _tenants.ListActiveStaffAsync(cancellationToken);

        if (staff.Count == 0)
        {
            return "Create and sign in the first SuperUser before seeding demo billing.";
        }

        Guid actor = staff[0].PlatformUserId;
        DateTimeOffset now = _billing.UtcNow;
        await AddProfileAndRatesAsync(actor, now, cancellationToken);

        PagedResult<PlatformTenantListItem> tenants = await _tenants.ListAsync(
            new PlatformTenantListFilter(null, null, PlatformDemoTenants.Tag, null),
            PagedRequest.Create(1, PagedRequest.MaximumPageSize),
            cancellationToken);

        int issued = 0;
        int paid = 0;

        foreach (PlatformTenantListItem item in tenants.Items.OrderBy(tenant => tenant.TenantCode, StringComparer.Ordinal))
        {
            PlatformSubscription? subscription = await _subscriptions.GetCurrentAsync(item.PlatformTenantId, cancellationToken);

            // Awaiting its first payment (seeded without the outbox, so no invoice was ever issued): the Operations demo companies are paid,
            // the others get their first-period invoice, as the billing handler would issue it (owner's report, 2026-10-21).
            if (subscription is { Status: PlatformSubscriptionStatus.PendingPayment, PeriodStartUtc: null })
            {
                if (PlatformDemoTenants.OperationsSlugs.Contains(item.Slug))
                {
                    _subscriptions.AddEvents(new[] { subscription.RecordOfflinePayment("Bank transfer received (demo).", actor, now) });
                    await SaveAsync(cancellationToken);
                }
                else
                {
                    issued += await FirstPeriodAsync(item, subscription, cancellationToken) ? 1 : 0;
                    continue;
                }
            }

            if (subscription is not { Status: PlatformSubscriptionStatus.Active or PlatformSubscriptionStatus.PastDue or PlatformSubscriptionStatus.PendingPayment, PeriodStartUtc: { } start }
                || await _tenantRecords.GetAsync(item.PlatformTenantId, cancellationToken) is not { } tenant
                || await _plans.GetAsync(subscription.PlatformPlanId, cancellationToken) is not { } plan
                || plan.Versions.FirstOrDefault(version => version.Id == subscription.PlanVersionId) is not { } version)
            {
                continue;
            }

            string sourceKey = "demo:" + subscription.Id.ToString("N", CultureInfo.InvariantCulture) + ":" + start.UtcTicks.ToString(CultureInfo.InvariantCulture);

            if (await _billing.Invoices.SourceBilledAsync(sourceKey, cancellationToken))
            {
                continue;
            }

            try
            {
                PlatformInvoice invoice = await _issuer.IssueAsync(
                    tenant,
                    subscription.Id,
                    PlatformInvoiceKind.Renewal,
                    sourceKey,
                    version.CurrencyCode,
                    subscription.PeriodStartUtc,
                    subscription.PeriodEndUtc,
                    new[] { Period(plan, tenant, subscription, version.PricePerLicence(subscription.Term)) },
                    cancellationToken);

                // Most demo invoices are settled; the third is part-paid and the rest stay open. The Operations demo companies always pay in full.
                if (PlatformDemoTenants.OperationsSlugs.Contains(item.Slug) || (issued != 2 && issued < 4))
                {
                    await PayAsync(invoice, invoice.Balance, actor, now, cancellationToken);
                    paid++;
                }
                else if (issued == 2)
                {
                    await PayAsync(invoice, PlatformBillingLimits.Money(invoice.Balance / 2m), actor, now, cancellationToken);
                }

                await SaveAsync(cancellationToken);
                issued++;
            }
            catch (DomainException)
            {
                _unitOfWork.DiscardChanges();
            }
        }

        return $"Demo billing: issued {issued} invoice(s), {paid} paid in full.";
    }

    private async Task<bool> FirstPeriodAsync(PlatformTenantListItem item, PlatformSubscription subscription, CancellationToken cancellationToken)
    {
        string sourceKey = "demo-first:" + subscription.Id.ToString("N", CultureInfo.InvariantCulture);

        if (await _billing.Invoices.SourceBilledAsync(sourceKey, cancellationToken) || await _billing.Invoices.FindOpenFirstPeriodAsync(item.PlatformTenantId, cancellationToken) is not null
            || await _tenantRecords.GetAsync(item.PlatformTenantId, cancellationToken) is not { } tenant
            || await _plans.GetAsync(subscription.PlatformPlanId, cancellationToken) is not { } plan
            || plan.Versions.FirstOrDefault(version => version.Id == subscription.PlanVersionId) is not { } version)
        {
            return false;
        }

        Dictionary<string, string> arguments = new(StringComparer.Ordinal)
        {
            ["plan"] = plan.NameIn(tenant.DefaultLanguage),
            ["term"] = subscription.Term.ToString(),
            ["licences"] = subscription.Licences.ToString(CultureInfo.InvariantCulture)
        };
        List<PlatformInvoiceDraftLine> lines = new()
        {
            new PlatformInvoiceDraftLine(PlatformInvoiceLineKind.PlanPeriod, "ui.invoice_line.plan_period", arguments, subscription.Licences, version.PricePerLicence(subscription.Term))
        };

        if (!subscription.SetupFeeCharged && subscription.SetupFeeFor(version) is > 0m and decimal fee)
        {
            lines.Add(new PlatformInvoiceDraftLine(PlatformInvoiceLineKind.SetupFee, "ui.invoice_line.setup_fee",
                new Dictionary<string, string>(StringComparer.Ordinal) { ["plan"] = plan.NameIn(tenant.DefaultLanguage) }, 1m, fee));
        }

        try
        {
            await _issuer.IssueAsync(tenant, subscription.Id, PlatformInvoiceKind.FirstPeriod, sourceKey, version.CurrencyCode, null, null, lines, cancellationToken);
            await SaveAsync(cancellationToken);
            return true;
        }
        catch (DomainException)
        {
            _unitOfWork.DiscardChanges();
            return false;
        }
    }

    private async Task AddProfileAndRatesAsync(Guid actor, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (await _billing.Store.GetProfileAsync(cancellationToken) is null)
        {
            _billing.Store.AddProfile(PlatformCompanyProfile.Create(
                PlatformCompanyDetails.Create(
                    "Vixly Logistics Software Inc.",
                    "Vixly",
                    EmailAddress.Create("billing@vixly.example"),
                    PhoneNumber.Create("+1 416 555 0100"),
                    PlatformPostalAddress.Create("100 King Street West", "Suite 5600", "Toronto", "ON", "M5X 1C9", "CA")),
                "123456789 RT0001",
                "https://vixly.example",
                actor,
                now));
        }

        if ((await _billing.Store.ListTaxRatesAsync(null, cancellationToken)).Count == 0)
        {
            DateTimeOffset from = new(now.Year - 1, 1, 1, 0, 0, 0, TimeSpan.Zero);
            _billing.Store.AddTaxRate(PlatformTaxRate.Create("CA", null, 5m, "GST", from, actor, now));
            _billing.Store.AddTaxRate(PlatformTaxRate.Create("CA", "QC", 14.975m, "GST+QST", from, actor, now));
            _billing.Store.AddTaxRate(PlatformTaxRate.Create("CA", "ON", 13m, "HST", from, actor, now));
            _billing.Store.AddTaxRate(PlatformTaxRate.Create("ES", null, 21m, "IVA", from, actor, now));
            _billing.Store.AddTaxRate(PlatformTaxRate.Create("FR", null, 20m, "TVA", from, actor, now));
        }

        await SaveAsync(cancellationToken);
    }

    private async Task PayAsync(PlatformInvoice invoice, decimal amount, Guid actor, DateTimeOffset now, CancellationToken cancellationToken)
    {
        string receipt = await _billing.NextNumberAsync(PlatformBillingDocumentKind.Receipt, cancellationToken);
        PlatformInvoicePaymentResult result = invoice.RecordPayment(receipt, amount, PlatformPaymentMethod.BankTransfer, now, "DEMO-" + invoice.Number, null, actor, now);
        _billing.Record(null, invoice, result.Event, "platform.billing.payment_recorded", null);
        await _billing.StoreReceiptDocumentAsync(invoice, result.Payment, cancellationToken);
    }

    private static PlatformInvoiceDraftLine Period(PlatformPlan plan, PlatformTenant tenant, PlatformSubscription subscription, decimal price)
    {
        Dictionary<string, string> arguments = new(StringComparer.Ordinal)
        {
            ["plan"] = plan.NameIn(tenant.DefaultLanguage),
            ["term"] = subscription.Term.ToString(),
            ["licences"] = subscription.Licences.ToString(CultureInfo.InvariantCulture),
            ["from"] = subscription.PeriodStartUtc!.Value.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["to"] = subscription.PeriodEndUtc?.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty
        };

        return new PlatformInvoiceDraftLine(PlatformInvoiceLineKind.PlanPeriod, "ui.invoice_line.plan_period_dates", arguments, subscription.Licences, price);
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            _unitOfWork.DiscardChanges();
            throw new DomainException(Vixly.Platform.Application.Security.PlatformCommonErrors.ConcurrencyConflict);
        }
    }
}
