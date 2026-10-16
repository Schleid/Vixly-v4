using System.Globalization;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformSubscriptionBillingHandler.cs
//
// PURPOSE:
// Turns what a subscription charges into an invoice (D-B3): the first period, an upgrade charged now, a renewal.
//
// RESPONSIBILITIES:
// - Read the subscription message and decide what is due
// - Add pending one-time charges to first-period and renewal invoices and mark them billed
// - Issue the invoice in its own commit; one invoice per subscription event
//
// IMPORTANT NOTES:
// - Idempotent: the event's source key is unique, so a replayed message bills nothing twice
// - A failed commit is retried by the outbox; one tenant failing never stops another
// - Nothing is invoiced when the total is zero (Slice 3 activates at once, D-S25)
// =========================================================
public abstract class PlatformSubscriptionBillingHandler : IPlatformOutboxHandler
{
    private readonly IPlatformSubscriptionRepository _subscriptions;

    private readonly IPlatformPlanRepository _plans;

    private readonly IPlatformTenantRepository _tenants;

    private readonly PlatformBillingAdministration _billing;

    private readonly PlatformInvoiceIssuer _issuer;

    private readonly IPlatformUnitOfWork _unitOfWork;

    protected PlatformSubscriptionBillingHandler(
        IPlatformSubscriptionRepository subscriptions,
        IPlatformPlanRepository plans,
        IPlatformTenantRepository tenants,
        PlatformBillingAdministration billing,
        PlatformInvoiceIssuer issuer,
        IPlatformUnitOfWork unitOfWork)
    {
        _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public abstract string MessageType { get; }

    public async Task HandleAsync(PlatformOutboxMessage message, CancellationToken cancellationToken)
    {
        PlatformSubscriptionMessage change = PlatformOutboxWriter.Read<PlatformSubscriptionMessage>(message);
        PlatformInvoiceKind? kind = change.EventCode switch
        {
            PlatformSubscriptionEventCodes.StartedAwaitingPayment or PlatformSubscriptionEventCodes.TrialEnded or PlatformSubscriptionEventCodes.TrialExpired => PlatformInvoiceKind.FirstPeriod,
            PlatformSubscriptionEventCodes.Changed when change.ChargeNow > 0m => PlatformInvoiceKind.Change,
            PlatformSubscriptionEventCodes.Renewed => PlatformInvoiceKind.Renewal,
            _ => null
        };

        if (kind is null)
        {
            return;
        }

        string sourceKey = string.Create(CultureInfo.InvariantCulture, $"sub:{change.PlatformSubscriptionId:N}:{change.EventCode[(change.EventCode.LastIndexOf('.') + 1)..]}:{change.OccurredUtc.UtcTicks}");

        if (await _billing.Invoices.SourceBilledAsync(sourceKey, cancellationToken))
        {
            return;
        }

        PlatformSubscription? subscription = await _subscriptions.GetAsync(change.PlatformSubscriptionId, cancellationToken);
        PlatformPlan? plan = subscription is null ? null : await _plans.GetAsync(subscription.PlatformPlanId, cancellationToken);
        PlatformTenant? tenant = await _tenants.GetAsync(change.PlatformTenantId, cancellationToken);

        if (subscription is null || plan is null || tenant is null)
        {
            return;
        }

        LanguageCodeName language = new(tenant);
        List<PlatformInvoiceDraftLine> lines = new();
        DateTimeOffset? from = null;
        DateTimeOffset? to = null;
        bool withCharges = false;

        switch (kind)
        {
            case PlatformInvoiceKind.FirstPeriod:
            {
                if (subscription.Status != PlatformSubscriptionStatus.PendingPayment)
                {
                    return;
                }

                PlatformPlanVersion version = plan.Version(subscription.PlanVersionId);
                decimal amount = PlatformPricing.PeriodAmount(version, subscription.Term, subscription.Licences);
                lines.Add(PeriodLine(plan, language, subscription.Term, subscription.Licences, version.PricePerLicence(subscription.Term), null, null));
                AddDiscount(lines, subscription.NextPeriodOff(amount, _billing.UtcNow));

                if (!subscription.SetupFeeCharged && subscription.SetupFeeFor(version) is > 0m and decimal fee)
                {
                    lines.Add(new PlatformInvoiceDraftLine(PlatformInvoiceLineKind.SetupFee, "ui.invoice_line.setup_fee", Arguments(("plan", language.Of(plan))), 1m, fee));
                }

                withCharges = true;
                break;
            }

            case PlatformInvoiceKind.Change:
                from = change.OccurredUtc;
                to = change.PeriodEndUtc;
                lines.Add(new PlatformInvoiceDraftLine(
                    PlatformInvoiceLineKind.ChangeProration,
                    "ui.invoice_line.change",
                    Arguments(("plan", language.Of(plan)), ("licences", change.Licences.ToString(CultureInfo.InvariantCulture)), ("to", Stamp(change.PeriodEndUtc))),
                    1m,
                    change.ChargeNow));
                break;

            default:
            {
                PlatformPlanVersion version = plan.Version(change.PlanVersionId);
                from = change.PeriodStartUtc;
                to = change.PeriodEndUtc;
                lines.Add(PeriodLine(plan, language, change.Term, change.Licences, version.PricePerLicence(change.Term), from, to));
                AddDiscount(lines, change.DiscountAmount);
                withCharges = true;
                break;
            }
        }

        PlatformOneTimeCharge[] charges = withCharges ? subscription.PendingCharges.ToArray() : Array.Empty<PlatformOneTimeCharge>();
        lines.AddRange(charges.Select(charge => new PlatformInvoiceDraftLine(
            PlatformInvoiceLineKind.OneTimeCharge, "ui.invoice_line.charge", Arguments(("description", charge.Description)), 1m, charge.Amount, charge.Id)));

        if (lines.Sum(line => line.Quantity * line.UnitPrice) <= 0m)
        {
            return;
        }

        await _issuer.IssueAsync(tenant, subscription.Id, kind.Value, sourceKey, change.CurrencyCode, from, to, lines, cancellationToken);

        if (charges.Length > 0)
        {
            subscription.MarkChargesBilled(charges.Select(charge => charge.Id), _billing.UtcNow);
        }

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            _unitOfWork.DiscardChanges();
            throw new DomainException(Vixly.Platform.Application.Security.PlatformCommonErrors.ConcurrencyConflict);
        }
    }

    private static PlatformInvoiceDraftLine PeriodLine(PlatformPlan plan, LanguageCodeName language, PlatformSubscriptionTerm term, int licences, decimal price, DateTimeOffset? from, DateTimeOffset? to)
    {
        List<(string, string)> arguments = new()
        {
            ("plan", language.Of(plan)),
            ("term", term.ToString()),
            ("licences", licences.ToString(CultureInfo.InvariantCulture))
        };

        if (from is not null && to is not null)
        {
            arguments.Add(("from", Stamp(from)));
            arguments.Add(("to", Stamp(to)));
        }

        return new PlatformInvoiceDraftLine(
            PlatformInvoiceLineKind.PlanPeriod,
            from is null ? "ui.invoice_line.plan_period" : "ui.invoice_line.plan_period_dates",
            Arguments(arguments.ToArray()),
            licences,
            price);
    }

    private static void AddDiscount(List<PlatformInvoiceDraftLine> lines, decimal off)
    {
        if (off > 0m)
        {
            lines.Add(new PlatformInvoiceDraftLine(PlatformInvoiceLineKind.Discount, "ui.invoice_line.discount", Arguments(), 1m, -off));
        }
    }

    private static Dictionary<string, string> Arguments(params (string Name, string Value)[] values)
    {
        return values.ToDictionary(value => value.Name, value => value.Value, StringComparer.Ordinal);
    }

    private static string Stamp(DateTimeOffset? value)
    {
        return value?.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private sealed class LanguageCodeName
    {
        private readonly PlatformTenant _tenant;

        public LanguageCodeName(PlatformTenant tenant)
        {
            _tenant = tenant;
        }

        public string Of(PlatformPlan plan)
        {
            return plan.NameIn(_tenant.DefaultLanguage);
        }
    }
}
