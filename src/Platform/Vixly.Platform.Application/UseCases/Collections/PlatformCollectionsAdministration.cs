using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Collections;
using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Collections;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Collections;

// =========================================================
// FILE: PlatformCollectionsAdministration.cs
//
// PURPOSE:
// The collections engine: looks at one tenant, moves its case along the timeline, blocks and clears, and retries cards (D-C1 to D-C10).
//
// RESPONSIBILITIES:
// - Load or create the settings row
// - Check one tenant: open, hold, advance, block, record no access, end extensions, close
// - Clear a block or Past due left by a credit note
// - Retry an opted-in card on its retry day, once per invoice and day
// - Write the emails to the outbox in the same commit as the step
//
// IMPORTANT NOTES:
// - Used by the clock (no actor) and by "check again now" (staff actor)
// - One tenant per commit; a lost race is undone and left for the next run (D-C18)
// - Customers are never touched: only the subscription status and access level change (D-C4)
// =========================================================
public sealed class PlatformCollectionsAdministration
{
    private readonly PlatformBillingAdministration _billing;

    private readonly PlatformSubscriptionAdministration _subscriptions;

    private readonly PlatformPaymentsAdministration _payments;

    private readonly IPlatformCollectionsRepository _store;

    private readonly IPlatformCollectionsQueries _queries;

    private readonly IPlatformTenantRepository _tenants;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public PlatformCollectionsAdministration(
        PlatformBillingAdministration billing,
        PlatformSubscriptionAdministration subscriptions,
        PlatformPaymentsAdministration payments,
        IPlatformCollectionsRepository store,
        IPlatformCollectionsQueries queries,
        IPlatformTenantRepository tenants,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        IPlatformZelleRepository zelle)
    {
        _zelle = zelle ?? throw new ArgumentNullException(nameof(zelle));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    private readonly IPlatformZelleRepository _zelle;

    private int _changes;

    public DateTimeOffset UtcNow => _billing.UtcNow;

    public IPlatformCollectionsRepository Store => _store;

    public IPlatformCollectionsQueries Queries => _queries;

    public IPlatformTenantRepository Tenants => _tenants;

    public PlatformBillingAdministration Billing => _billing;

    public PlatformSubscriptionAdministration Subscriptions => _subscriptions;

    public PlatformPaymentsAdministration Payments => _payments;

    public Task<Error?> AuthorizeAsync(PlatformAuthorizationContext context, string permissionCode, bool requireStepUp, CancellationToken cancellationToken)
    {
        return _billing.AuthorizeAsync(context, permissionCode, requireStepUp, cancellationToken);
    }

    public Task<bool> HoldsAsync(PlatformAuthorizationContext context, string permissionCode, CancellationToken cancellationToken)
    {
        return _billing.HoldsAsync(context, permissionCode, cancellationToken);
    }

    public void Audit(PlatformAuthorizationContext? context, string actionCode, string? permissionCode, string targetType, Guid? targetId, object? after = null, string? reason = null)
    {
        _audit.Record(context?.PlatformUserId, actionCode, permissionCode, targetType, targetId, after: after, reason: reason);
    }

    public async Task<bool> SaveAsync(CancellationToken cancellationToken)
    {
        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return true;
        }

        _unitOfWork.DiscardChanges();
        return false;
    }

    public void Discard()
    {
        _unitOfWork.DiscardChanges();
    }

    public async Task<PlatformCollectionsSettings> SettingsAsync(CancellationToken cancellationToken)
    {
        PlatformCollectionsSettings? settings = await _store.FindSettingsAsync(cancellationToken);

        if (settings is null)
        {
            settings = PlatformCollectionsSettings.Create(UtcNow);
            _store.AddSettings(settings);
        }

        return settings;
    }

    public void Notice(Guid tenantId, Guid? caseId, PlatformCollectionsNoticeKind kind, DateTimeOffset? blockDateUtc = null, DateTime? extendedThrough = null)
    {
        _changes++;
        _billing.Outbox.Write(
            PlatformMessageTypes.CollectionsNotice,
            tenantId,
            new PlatformCollectionsNotice(tenantId, caseId, kind, blockDateUtc, extendedThrough, UtcNow));
    }

    public async Task RecordSubscriptionAsync(PlatformAuthorizationContext? context, PlatformSubscription subscription, PlatformSubscriptionEvent change, string? permissionCode, string actionCode, CancellationToken cancellationToken)
    {
        PlatformPlan plan = await _subscriptions.Plans.GetAsync(subscription.PlatformPlanId, cancellationToken)
            ?? throw new DomainException(PlatformCommonErrors.NotFound);
        _subscriptions.RecordChange(context, subscription, plan, change, permissionCode, actionCode);
    }

    // =====================================================
    // METHOD: CHECK ONE TENANT (the engine)
    // =====================================================
    // Returns true when anything was saved. Retries run after the step's commit, each in its own.
    public async Task<bool> CheckTenantAsync(PlatformAuthorizationContext? context, Guid tenantId, CancellationToken cancellationToken)
    {
        PlatformCollectionsSettings settings = await SettingsAsync(cancellationToken);
        DateTimeOffset utcNow = UtcNow;
        PlatformTenant? tenant = await _tenants.GetAsync(tenantId, cancellationToken);

        if (tenant is null)
        {
            return false;
        }

        PlatformCollectionCase? open = await _store.GetOpenCaseAsync(tenantId, cancellationToken);
        List<PlatformCollectionCaseEvent> events = new();
        int changesBefore = _changes;

        // A closed account ends its case; a suspended or not-yet-active one waits (D-C8).
        if (tenant.Status is PlatformTenantStatus.Deactivated or PlatformTenantStatus.Banned)
        {
            if (open is not null)
            {
                Close(open, PlatformCollectionCloseReason.TenantClosed, events);
                _store.AddEvents(events);
                return await SaveAsync(cancellationToken);
            }

            return false;
        }

        if (tenant.Status != PlatformTenantStatus.Active || !settings.Enabled)
        {
            return false;
        }

        PlatformSubscription? subscription = await _subscriptions.Subscriptions.GetCurrentAsync(tenantId, cancellationToken);
        IReadOnlyList<PlatformInvoice> overdue = await _billing.Invoices.ListOverdueForTenantAsync(tenantId, utcNow, cancellationToken);

        if (subscription is not null)
        {
            await ClearIfSettledAsync(subscription, overdue, cancellationToken);
        }

        if (open is not null && (overdue.Count == 0 || subscription is null))
        {
            bool wasBlocked = open.IsBlocked;
            Close(open, overdue.Count == 0 ? PlatformCollectionCloseReason.Settled : PlatformCollectionCloseReason.SubscriptionEnded, events);

            if (wasBlocked && overdue.Count == 0)
            {
                Notice(tenantId, open.Id, PlatformCollectionsNoticeKind.AccessRestored);
            }

            open = null;
        }
        else if (open is null && overdue.Count > 0 && subscription is not null)
        {
            (open, PlatformCollectionCaseEvent opened) = PlatformCollectionCase.Open(tenantId, subscription.Id, overdue.Min(invoice => invoice.DueUtc), utcNow);
            _store.AddCase(open);
            events.Add(opened);
            Audit(context, "platform.collections.case_opened", null, nameof(PlatformTenant), tenantId, new { due = open.OldestDueUtc });
        }

        bool blockedNow = false;

        if (open is not null)
        {
            bool held = open.Held;
            // A credit-note request or a Zelle payment report waiting for staff pauses the timeline.
            IReadOnlyList<Guid> zelleWaiting = await _zelle.ListWaitingInvoiceIdsAsync(tenantId, cancellationToken);
            open.Hold(overdue.Any(invoice => invoice.PendingCredit is not null || zelleWaiting.Contains(invoice.Id)), utcNow);
            _changes += held != open.Held ? 1 : 0;

            if (open.EndExtensionIfDue(utcNow) is { } ended)
            {
                events.Add(ended);
                Audit(context, "platform.collections.extension_ended", null, nameof(PlatformTenant), tenantId);
            }

            if (open.TakeExtensionEndingNotice(utcNow))
            {
                Notice(tenantId, open.Id, PlatformCollectionsNoticeKind.ExtensionEnding, extendedThrough: open.ExtendedThrough);
            }

            if (open.NextStep(settings, utcNow) is { } step)
            {
                events.Add(open.Advance(step, settings, utcNow));
                Notice(tenantId, open.Id, KindOf(step), open.PlannedBlockUtc);
                Audit(context, ActionOf(step), null, nameof(PlatformTenant), tenantId, new { day = open.DayAt(utcNow), blockDate = open.PlannedBlockUtc });

                if (step == PlatformCollectionStep.Block && subscription is not null)
                {
                    blockedNow = await BlockAsync(context, subscription, overdue.OrderBy(invoice => invoice.DueUtc).First().Number, cancellationToken);
                }
            }

            if (open.RecordNoAccessIfDue(settings, utcNow) is { } noAccess)
            {
                events.Add(noAccess);
                Audit(context, "platform.collections.no_access", null, nameof(PlatformTenant), tenantId);
            }

            // Nothing happened: no write, so a quiet run costs one read per tenant.
            if (events.Count > 0 || _changes != changesBefore)
            {
                open.Checked(utcNow);
            }
        }

        // Pending payment past its read-only days is blocked too (D-C5), but only when there is an
        // overdue invoice to pay: a tenant is never blocked with nothing it could pay.
        if (!blockedNow && subscription is { Status: PlatformSubscriptionStatus.PendingPayment }
            && utcNow >= subscription.StatusChangedUtc.AddDays(settings.PendingPaymentReadOnlyDays)
            && overdue.Any(invoice => invoice.PlatformSubscriptionId == subscription.Id)
            && await BlockAsync(context, subscription, overdue.OrderBy(invoice => invoice.DueUtc).FirstOrDefault()?.Number, cancellationToken))
        {
            Notice(tenantId, open?.Id, PlatformCollectionsNoticeKind.Blocked);
            Audit(context, "platform.collections.blocked", null, nameof(PlatformTenant), tenantId, new { pendingPayment = true });
        }

        bool changed = events.Count > 0 || _changes != changesBefore;
        _store.AddEvents(events);

        if (changed && !await SaveAsync(cancellationToken))
        {
            return false;
        }

        if (open is { IsOpen: true } && !open.IsBlocked && !open.Held)
        {
            foreach (PlatformInvoice invoice in overdue)
            {
                changed |= await RetryAsync(open, invoice, settings, cancellationToken);
            }
        }

        return changed;
    }

    // Payments clear a block in their own commit (D-C6); a credit note that settles the bill is caught here.
    private async Task ClearIfSettledAsync(PlatformSubscription subscription, IReadOnlyList<PlatformInvoice> overdue, CancellationToken cancellationToken)
    {
        if (subscription.Status is not (PlatformSubscriptionStatus.BlockedForNonPayment or PlatformSubscriptionStatus.PastDue)
            || overdue.Any(invoice => invoice.PlatformSubscriptionId == subscription.Id)
            || (subscription.Status == PlatformSubscriptionStatus.BlockedForNonPayment && subscription.StatusBeforeBlock == PlatformSubscriptionStatus.PendingPayment))
        {
            return;
        }

        PlatformSubscriptionEvent cleared = subscription.Activate(PlatformSubscriptionEventCodes.InvoicePaid, null, null, UtcNow);
        await RecordSubscriptionAsync(null, subscription, cleared, null, "platform.subscriptions.invoice_paid", cancellationToken);
        _changes++;
    }

    private async Task<bool> BlockAsync(PlatformAuthorizationContext? context, PlatformSubscription subscription, string? invoiceNumber, CancellationToken cancellationToken)
    {
        if (subscription.BlockForNonPayment(invoiceNumber, UtcNow) is not { } blocked)
        {
            return false;
        }

        await RecordSubscriptionAsync(context, subscription, blocked, null, "platform.subscriptions.blocked_for_non_payment", cancellationToken);
        _changes++;
        return true;
    }

    private void Close(PlatformCollectionCase open, PlatformCollectionCloseReason reason, List<PlatformCollectionCaseEvent> events)
    {
        events.AddRange(open.Close(reason, UtcNow));
        Audit(null, "platform.collections.case_closed", null, nameof(PlatformTenant), open.PlatformTenantId, new { reason = reason.ToString() });
    }

    // =====================================================
    // METHOD: CARD RETRIES (D-C9)
    // =====================================================
    private async Task<bool> RetryAsync(PlatformCollectionCase open, PlatformInvoice invoice, PlatformCollectionsSettings settings, CancellationToken cancellationToken)
    {
        int day = PlatformCollectionsLimits.DayOf(invoice.DueUtc, UtcNow);
        int[] due = settings.RetryDayList.Where(retryDay => retryDay <= day).ToArray();

        if (due.Length == 0)
        {
            return false;
        }

        // After downtime only the latest retry day is tried, once.
        int retryDay = due.Max();
        string key = PlatformCollectionCase.RetryKeyFor(invoice.Id, retryDay);

        if (await _store.RetryRecordedAsync(key, cancellationToken)
            || await _payments.RetryPlanAsync(invoice, cancellationToken) is not { } plan)
        {
            return false;
        }

        try
        {
            // The retry is claimed first (unique key), so two hosts never charge the same day twice.
            _store.AddEvents(new[] { open.RecordRetry(invoice.Id, invoice.Number, retryDay, UtcNow) });
            Audit(null, "platform.collections.card_retry", null, nameof(PlatformInvoice), invoice.Id, new { number = invoice.Number, day = retryDay });

            if (!await SaveAsync(cancellationToken))
            {
                return false;
            }

            await _payments.RetryChargeAsync(invoice, plan.Profile, plan.Card, cancellationToken);
            return true;
        }
        catch (DomainException)
        {
            Discard();
            return false;
        }
        catch (PlatformCardGatewayException)
        {
            Discard();
            return false;
        }
    }

    private static PlatformCollectionsNoticeKind KindOf(PlatformCollectionStep step)
    {
        return step switch
        {
            PlatformCollectionStep.Reminder1 => PlatformCollectionsNoticeKind.Reminder1,
            PlatformCollectionStep.Reminder2 => PlatformCollectionsNoticeKind.Reminder2,
            PlatformCollectionStep.FinalWarning => PlatformCollectionsNoticeKind.FinalWarning,
            _ => PlatformCollectionsNoticeKind.Blocked
        };
    }

    private static string ActionOf(PlatformCollectionStep step)
    {
        return step switch
        {
            PlatformCollectionStep.Reminder1 or PlatformCollectionStep.Reminder2 => "platform.collections.reminder_sent",
            PlatformCollectionStep.FinalWarning => "platform.collections.final_warning_sent",
            _ => "platform.collections.blocked"
        };
    }
}
