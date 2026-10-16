using System.Globalization;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformSubscription.cs
//
// PURPOSE:
// A tenant's subscription to a plan (D-S3 to D-S6, D-S12).
//
// RESPONSIBILITIES:
// - Start in a trial or awaiting payment
// - Quote and apply plan, term and licence changes: now or at the period end (D-S4, D-S5)
// - Run trials, payments, renewals, cancellation and the reactions to tenant status (D-S10)
// - Say what the tenant may do (D-S14)
//
// IMPORTANT NOTES:
// - Aggregate root. Each move returns its history event; the use case saves it with the audit entry and the outbox message (D-2)
// - One current subscription per tenant: the use case checks, a filtered unique index guarantees (D-S3)
// - The subscription keeps its plan version for the whole period; renewal moves it to the current version (owner, D-S2)
// - Cancellation is a flag: the subscription stays Active until the period end
// - One active discount at a time, plus at most one waiting for approval (D-S18, D-S24).
//   A discount applies to periods that start while it is active
// =========================================================
public sealed class PlatformSubscription : AuditableEntity
{
    private const int MaxRenewalsAtOnce = 24;

    private readonly List<PlatformSubscriptionDiscount> _discounts = new();

    private readonly List<PlatformOneTimeCharge> _charges = new();

    private PlatformSubscription()
    {
    }

    private PlatformSubscription(
        Guid platformTenantId,
        Guid platformPlanId,
        Guid planVersionId,
        PlatformSubscriptionTerm term,
        int licences,
        DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        PlatformTenantId = platformTenantId;
        PlatformPlanId = platformPlanId;
        PlanVersionId = planVersionId;
        Term = term;
        Licences = licences;
        StatusChangedUtc = createdUtc;
    }

    public Guid PlatformTenantId { get; private set; }

    public Guid PlatformPlanId { get; private set; }

    public Guid PlanVersionId { get; private set; }

    public PlatformSubscriptionTerm Term { get; private set; }

    public int Licences { get; private set; }

    public PlatformSubscriptionStatus Status { get; private set; }

    public DateTimeOffset StatusChangedUtc { get; private set; }

    public DateTimeOffset? TrialEndsUtc { get; private set; }

    public DateTimeOffset? PeriodStartUtc { get; private set; }

    public DateTimeOffset? PeriodEndUtc { get; private set; }

    public bool SetupFeeCharged { get; private set; }

    public bool CancelAtPeriodEnd { get; private set; }

    public DateTimeOffset? CancellationRequestedUtc { get; private set; }

    public PlatformSubscriptionStatus? StatusBeforeSuspension { get; private set; }

    public PlatformSubscriptionStatus? StatusBeforeBlock { get; private set; }

    public DateTimeOffset? BlockedUtc { get; private set; }

    public DateTimeOffset? EndedUtc { get; private set; }

    public PlatformSubscriptionEndReason? EndReason { get; private set; }

    public PlatformSubscriptionScheduledChange? ScheduledChange { get; private set; }

    public decimal? SetupFeeOverride { get; private set; }

    public IReadOnlyList<PlatformSubscriptionDiscount> Discounts => _discounts;

    public IReadOnlyList<PlatformOneTimeCharge> Charges => _charges;

    public PlatformSubscriptionDiscount? ActiveDiscount => _discounts.FirstOrDefault(item => item.IsActive);

    public PlatformSubscriptionDiscount? PendingDiscount => _discounts.FirstOrDefault(item => item.IsPending);

    public bool IsCurrent => Status != PlatformSubscriptionStatus.Ended;

    // =====================================================
    // METHOD: START
    // =====================================================
    public static PlatformSubscriptionStart Start(
        Guid platformTenantId,
        PlatformPlan plan,
        PlatformSubscriptionTerm term,
        int licences,
        int maxLicences,
        bool startTrial,
        Guid? actorUserId,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (platformTenantId == Guid.Empty)
        {
            throw new DomainException(PlatformSubscriptionErrors.TenantRequired);
        }

        PlatformPlanVersion version = Chosen(plan);
        CheckTermAndLicences(version, term, licences, maxLicences, activeLicences: 0);

        if (startTrial && !version.OffersTrial)
        {
            throw new DomainException(PlatformSubscriptionErrors.TrialNotOffered);
        }

        PlatformSubscription subscription = new(platformTenantId, plan.Id, version.Id, term, licences, utcNow);
        Dictionary<string, string> arguments = Describe(plan.Code, version.Number, term, licences);

        if (startTrial)
        {
            subscription.Status = PlatformSubscriptionStatus.Trialing;
            subscription.TrialEndsUtc = utcNow.AddDays(version.TrialDays!.Value);
            arguments["trialEnds"] = Stamp(subscription.TrialEndsUtc.Value);

            return new PlatformSubscriptionStart(
                subscription,
                PlatformSubscriptionEvent.Record(subscription, PlatformSubscriptionEventCodes.StartedTrial, null, actorUserId, utcNow, arguments: arguments));
        }

        subscription.Status = PlatformSubscriptionStatus.PendingPayment;

        return new PlatformSubscriptionStart(
            subscription,
            PlatformSubscriptionEvent.Record(subscription, PlatformSubscriptionEventCodes.StartedAwaitingPayment, null, actorUserId, utcNow, arguments: arguments));
    }

    // =====================================================
    // METHOD: ACCESS (D-S14)
    // =====================================================
    // Unpaid (Pending payment or Blocked): read-only for the period in settings, then payment
    // required. Suspended (account) and Ended: none. Customers are never affected (D-C4).
    public PlatformSubscriptionAccess AccessAt(DateTimeOffset utcNow, PlatformSubscriptionAccessRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        return Status switch
        {
            PlatformSubscriptionStatus.Trialing or PlatformSubscriptionStatus.Active or PlatformSubscriptionStatus.PastDue => PlatformSubscriptionAccess.Full,
            PlatformSubscriptionStatus.PendingPayment => utcNow < StatusChangedUtc.Add(rules.UnpaidReadOnly)
                ? PlatformSubscriptionAccess.ReadOnly
                : PlatformSubscriptionAccess.PaymentRequired,
            PlatformSubscriptionStatus.BlockedForNonPayment => rules.BlockedReadOnly is not { } period || utcNow < (BlockedUtc ?? StatusChangedUtc).Add(period)
                ? PlatformSubscriptionAccess.ReadOnly
                : PlatformSubscriptionAccess.PaymentRequired,
            _ => PlatformSubscriptionAccess.None
        };
    }

    // =====================================================
    // METHOD: NON-PAYMENT (Slice 6, D-C4, D-C5, D-C7)
    // =====================================================
    public PlatformSubscriptionEvent? BlockForNonPayment(string? invoiceNumber, DateTimeOffset utcNow)
    {
        if (Status is not (PlatformSubscriptionStatus.Active or PlatformSubscriptionStatus.PastDue or PlatformSubscriptionStatus.PendingPayment))
        {
            return null;
        }

        PlatformSubscriptionStatus from = Status;
        StatusBeforeBlock = from;
        BlockedUtc = utcNow;
        MoveTo(PlatformSubscriptionStatus.BlockedForNonPayment, utcNow);

        Dictionary<string, string> arguments = new(StringComparer.Ordinal);

        if (invoiceNumber is not null)
        {
            arguments["invoice"] = invoiceNumber;
        }

        return PlatformSubscriptionEvent.Record(this, PlatformSubscriptionEventCodes.BlockedForNonPayment, from, null, utcNow, arguments: arguments);
    }

    // An extension lifts the block while it runs; the status before the block comes back.
    public PlatformSubscriptionEvent? LiftBlockForExtension(DateTime extendedThrough, Guid actorUserId, DateTimeOffset utcNow)
    {
        if (Status != PlatformSubscriptionStatus.BlockedForNonPayment)
        {
            return null;
        }

        PlatformSubscriptionStatus target = StatusBeforeBlock ?? PlatformSubscriptionStatus.PastDue;
        StatusBeforeBlock = null;
        BlockedUtc = null;
        MoveTo(target, utcNow);

        return PlatformSubscriptionEvent.Record(this, PlatformSubscriptionEventCodes.BlockLifted, PlatformSubscriptionStatus.BlockedForNonPayment, actorUserId, utcNow,
            arguments: new Dictionary<string, string>(StringComparer.Ordinal) { ["through"] = extendedThrough.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) });
    }

    // =====================================================
    // METHOD: PAYMENT
    // =====================================================
    public PlatformSubscriptionEvent RecordOfflinePayment(string? reason, Guid actorUserId, DateTimeOffset utcNow)
    {
        string why = PlatformSubscriptionText.Reason(reason);
        Guid actor = PlatformSubscriptionText.Actor(actorUserId);

        return Activate(PlatformSubscriptionEventCodes.OfflinePaymentRecorded, actor, why, utcNow);
    }

    // Billing (Slice 4): the invoice this subscription waits for is paid. Activates a
    // subscription awaiting payment, or clears Past due once nothing else is overdue (D-B8).
    public PlatformSubscriptionEvent? InvoicePaid(string invoiceNumber, bool otherInvoicesOverdue, DateTimeOffset utcNow)
    {
        if (Status == PlatformSubscriptionStatus.PendingPayment
            || (Status is PlatformSubscriptionStatus.PastDue or PlatformSubscriptionStatus.BlockedForNonPayment && !otherInvoicesOverdue))
        {
            return Activate(PlatformSubscriptionEventCodes.InvoicePaid, null, null, utcNow, invoiceNumber);
        }

        return null;
    }

    // Billing (Slice 4): a renewal or change invoice is unpaid after its due date (D-B8).
    public PlatformSubscriptionEvent? MarkPastDue(string invoiceNumber, DateTimeOffset utcNow)
    {
        if (Status != PlatformSubscriptionStatus.Active)
        {
            return null;
        }

        MoveTo(PlatformSubscriptionStatus.PastDue, utcNow);

        return PlatformSubscriptionEvent.Record(
            this,
            PlatformSubscriptionEventCodes.PastDue,
            PlatformSubscriptionStatus.Active,
            null,
            utcNow,
            arguments: new Dictionary<string, string>(StringComparer.Ordinal) { ["invoice"] = invoiceNumber });
    }

    public IReadOnlyList<PlatformOneTimeCharge> PendingCharges => _charges.Where(item => item.Status == PlatformOneTimeChargeStatus.Pending).ToArray();

    // Billing (Slice 4): the pending charges went onto an invoice.
    public void MarkChargesBilled(IEnumerable<Guid> chargeIds, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(chargeIds);

        foreach (Guid chargeId in chargeIds)
        {
            (_charges.FirstOrDefault(item => item.Id == chargeId) ?? throw new DomainException(PlatformDiscountErrors.ChargeNotFound)).MarkBilled(utcNow);
        }

        MarkAsModified(utcNow);
    }

    public PlatformSubscriptionEvent Activate(string eventCode, Guid? actorUserId, string? reason, DateTimeOffset utcNow, string? invoiceNumber = null)
    {
        PlatformSubscriptionStatus from = Status;

        if (from is not (PlatformSubscriptionStatus.PendingPayment or PlatformSubscriptionStatus.PastDue or PlatformSubscriptionStatus.BlockedForNonPayment))
        {
            throw new DomainException(PlatformSubscriptionErrors.NotAwaitingPayment);
        }

        // Paying clears a block (D-C6): it continues from the status before the block.
        PlatformSubscriptionStatus effective = from == PlatformSubscriptionStatus.BlockedForNonPayment
            ? StatusBeforeBlock ?? PlatformSubscriptionStatus.PastDue
            : from;
        StatusBeforeBlock = null;
        BlockedUtc = null;

        Dictionary<string, string> arguments = new(StringComparer.Ordinal);

        if (invoiceNumber is not null)
        {
            arguments["invoice"] = invoiceNumber;
        }

        if (effective == PlatformSubscriptionStatus.PendingPayment)
        {
            arguments["setupFee"] = SetupFeeCharged ? "false" : "true";
            SetupFeeCharged = true;
            PeriodStartUtc = utcNow;
            PeriodEndUtc = PlatformPricing.PeriodEnd(utcNow, Term);
            arguments["periodEnd"] = Stamp(PeriodEndUtc.Value);

            if (ActiveDiscount is { } discount && discount.AppliesToPeriodStarting(utcNow))
            {
                discount.UsePeriod(utcNow, utcNow);
            }
        }

        MoveTo(PlatformSubscriptionStatus.Active, utcNow);

        return PlatformSubscriptionEvent.Record(this, eventCode, from, actorUserId, utcNow, reason, arguments);
    }

    // =====================================================
    // METHOD: TRIAL (D-S12)
    // =====================================================
    public PlatformSubscriptionEvent ExtendTrial(DateTimeOffset newEndUtc, string? reason, Guid actorUserId, DateTimeOffset utcNow)
    {
        string why = PlatformSubscriptionText.Reason(reason);
        Guid actor = PlatformSubscriptionText.Actor(actorUserId);
        RequireTrialing();

        if (newEndUtc <= utcNow || newEndUtc > utcNow.Add(PlatformSubscriptionLimits.MaxTrialExtension) || newEndUtc.Offset != TimeSpan.Zero)
        {
            throw new DomainException(PlatformSubscriptionErrors.TrialEndInvalid);
        }

        Dictionary<string, string> arguments = new(StringComparer.Ordinal)
        {
            ["from"] = Stamp(TrialEndsUtc!.Value),
            ["to"] = Stamp(newEndUtc)
        };

        TrialEndsUtc = newEndUtc;
        MarkAsModified(utcNow);

        return PlatformSubscriptionEvent.Record(this, PlatformSubscriptionEventCodes.TrialExtended, Status, actor, utcNow, why, arguments);
    }

    public PlatformSubscriptionEvent EndTrial(string? reason, Guid actorUserId, DateTimeOffset utcNow)
    {
        string why = PlatformSubscriptionText.Reason(reason);
        Guid actor = PlatformSubscriptionText.Actor(actorUserId);
        RequireTrialing();

        TrialEndsUtc = utcNow;
        MoveTo(PlatformSubscriptionStatus.PendingPayment, utcNow);

        return PlatformSubscriptionEvent.Record(this, PlatformSubscriptionEventCodes.TrialEnded, PlatformSubscriptionStatus.Trialing, actor, utcNow, why);
    }

    public PlatformSubscriptionEvent? ExpireTrialIfDue(DateTimeOffset utcNow)
    {
        if (Status != PlatformSubscriptionStatus.Trialing || TrialEndsUtc > utcNow)
        {
            return null;
        }

        MoveTo(PlatformSubscriptionStatus.PendingPayment, utcNow);

        return PlatformSubscriptionEvent.Record(this, PlatformSubscriptionEventCodes.TrialExpired, PlatformSubscriptionStatus.Trialing, null, utcNow);
    }

    // =====================================================
    // METHOD: CHANGES (D-S4, D-S5)
    // =====================================================
    public PlatformSubscriptionQuote Quote(
        PlatformPlan currentPlan,
        PlatformPlan targetPlan,
        PlatformSubscriptionTerm term,
        int licences,
        int maxLicences,
        int activeLicences,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(currentPlan);
        ArgumentNullException.ThrowIfNull(targetPlan);
        RequireChangeable();

        PlatformPlanVersion currentVersion = currentPlan.Version(PlanVersionId);
        bool samePlan = targetPlan.Id == PlatformPlanId;
        bool paid = Status is PlatformSubscriptionStatus.Active or PlatformSubscriptionStatus.PastDue;

        if (!samePlan && !targetPlan.CanBeChosen)
        {
            throw new DomainException(PlatformSubscriptionErrors.PlanNotAvailable);
        }

        PlatformPlanVersion immediateVersion = samePlan ? currentVersion : Chosen(targetPlan);
        CheckTermAndLicences(immediateVersion, term, licences, maxLicences, activeLicences);

        if (samePlan && term == Term && licences == Licences)
        {
            throw new DomainException(PlatformSubscriptionErrors.NothingToChange);
        }

        bool now = !paid
            || (term == Term
                && licences >= Licences
                && (samePlan || immediateVersion.PricePerLicence(term) > currentVersion.PricePerLicence(Term)));

        if (now)
        {
            decimal charge = 0m;

            if (paid)
            {
                decimal ratio = PlatformPricing.RemainingRatio(PeriodStartUtc!.Value, PeriodEndUtc!.Value, utcNow);
                charge = samePlan
                    ? PlatformPricing.AddedLicencesCharge(currentVersion.PricePerLicence(Term), licences - Licences, ratio)
                    : PlatformPricing.UpgradeCharge(
                        PlatformPricing.PeriodAmount(currentVersion, Term, Licences),
                        PlatformPricing.PeriodAmount(immediateVersion, term, licences),
                        ratio);

                if (ActiveDiscount is { } current && current.AppliedToPeriod(PeriodStartUtc))
                {
                    charge = current.Terms.ReduceCharge(charge);
                }
            }

            decimal next = PlatformPricing.PeriodAmount(immediateVersion, term, licences);

            return new PlatformSubscriptionQuote(
                PlatformSubscriptionChangeTiming.Now,
                targetPlan.Id,
                targetPlan.Code,
                immediateVersion.Id,
                immediateVersion.Number,
                term,
                licences,
                charge,
                next,
                immediateVersion.CurrencyCode,
                utcNow,
                Status == PlatformSubscriptionStatus.Trialing,
                next - NextPeriodOff(next, utcNow));
        }

        PlatformPlanVersion laterVersion = Chosen(targetPlan, fallback: samePlan ? currentVersion : null);
        CheckTermAndLicences(laterVersion, term, licences, maxLicences, activeLicences);
        decimal later = PlatformPricing.PeriodAmount(laterVersion, term, licences);

        return new PlatformSubscriptionQuote(
            PlatformSubscriptionChangeTiming.AtPeriodEnd,
            targetPlan.Id,
            targetPlan.Code,
            laterVersion.Id,
            laterVersion.Number,
            term,
            licences,
            0m,
            later,
            laterVersion.CurrencyCode,
            PeriodEndUtc!.Value,
            false,
            later - NextPeriodOff(later, utcNow));
    }

    public PlatformSubscriptionEvent Apply(PlatformSubscriptionQuote quote, string currentPlanCode, Guid? actorUserId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(quote);
        RequireChangeable();

        PlatformSubscriptionStatus from = Status;
        Dictionary<string, string> arguments = Describe(quote.TargetPlanCode, quote.TargetVersionNumber, quote.TargetTerm, quote.TargetLicences);
        arguments["fromPlan"] = currentPlanCode;
        arguments["fromLicences"] = Licences.ToString(CultureInfo.InvariantCulture);
        arguments["fromTerm"] = Term.ToString();

        if (quote.Timing == PlatformSubscriptionChangeTiming.AtPeriodEnd)
        {
            ScheduledChange = new PlatformSubscriptionScheduledChange(
                quote.TargetPlanId,
                quote.TargetVersionId,
                quote.TargetTerm,
                quote.TargetLicences,
                quote.EffectiveUtc,
                actorUserId,
                utcNow);
            arguments["effective"] = Stamp(quote.EffectiveUtc);
            MarkAsModified(utcNow);

            return PlatformSubscriptionEvent.Record(this, PlatformSubscriptionEventCodes.ChangeScheduled, from, actorUserId, utcNow, arguments: arguments);
        }

        PlatformPlanId = quote.TargetPlanId;
        PlanVersionId = quote.TargetVersionId;
        Term = quote.TargetTerm;
        Licences = quote.TargetLicences;
        ScheduledChange = null;
        arguments["charge"] = quote.ChargeNow.ToString(CultureInfo.InvariantCulture);

        if (quote.EndsTrial)
        {
            TrialEndsUtc = utcNow;
            MoveTo(PlatformSubscriptionStatus.PendingPayment, utcNow);
        }
        else
        {
            MarkAsModified(utcNow);
        }

        return PlatformSubscriptionEvent.Record(this, PlatformSubscriptionEventCodes.Changed, from, actorUserId, utcNow, arguments: arguments);
    }

    public PlatformSubscriptionEvent CancelScheduledChange(Guid? actorUserId, DateTimeOffset utcNow)
    {
        if (ScheduledChange is null)
        {
            throw new DomainException(PlatformSubscriptionErrors.NoScheduledChange);
        }

        ScheduledChange = null;
        MarkAsModified(utcNow);

        return PlatformSubscriptionEvent.Record(this, PlatformSubscriptionEventCodes.ScheduledChangeCancelled, Status, actorUserId, utcNow);
    }

    // =====================================================
    // METHOD: RENEWAL (owner, D-S2)
    // =====================================================
    public bool IsRenewalDue(DateTimeOffset utcNow)
    {
        return Status is PlatformSubscriptionStatus.Active or PlatformSubscriptionStatus.PastDue
            && PeriodEndUtc <= utcNow;
    }

    public PlatformSubscriptionRenewal? RenewIfDue(PlatformPlan currentPlan, PlatformPlan? scheduledPlan, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(currentPlan);

        if (!IsRenewalDue(utcNow))
        {
            return null;
        }

        PlatformSubscriptionStatus from = Status;
        DateTimeOffset boundary = PeriodEndUtc!.Value;

        if (CancelAtPeriodEnd)
        {
            PlatformSubscriptionEvent ended = End(PlatformSubscriptionEndReason.Cancelled, boundary, null, null, utcNow);

            return new PlatformSubscriptionRenewal(ended, true, PlanVersionId, null, null, 0m, currentPlan.CurrencyCode);
        }

        PlatformPlan plan = currentPlan;

        if (ScheduledChange is { } change)
        {
            plan = scheduledPlan is not null && scheduledPlan.Id == change.TargetPlanId
                ? scheduledPlan
                : throw new DomainException(PlatformSubscriptionErrors.PlanNotAvailable);
            PlatformPlanId = change.TargetPlanId;
            PlanVersionId = (plan.Current ?? plan.Version(change.TargetVersionId)).Id;
            Term = change.TargetTerm;
            Licences = change.TargetLicences;
            ScheduledChange = null;
        }
        else
        {
            PlanVersionId = (plan.Current ?? plan.Version(PlanVersionId)).Id;
        }

        int periods = 0;
        PlatformSubscriptionEvent? discountEnded = null;

        while (PeriodEndUtc <= utcNow && periods < MaxRenewalsAtOnce)
        {
            PeriodStartUtc = PeriodEndUtc;
            PeriodEndUtc = PlatformPricing.PeriodEnd(PeriodStartUtc!.Value, Term);
            periods++;

            if (ActiveDiscount is { } discount)
            {
                if (discount.AppliesToPeriodStarting(PeriodStartUtc.Value))
                {
                    discount.UsePeriod(PeriodStartUtc.Value, utcNow);
                }
                else
                {
                    discount.End(PlatformDiscountEndReason.Completed, utcNow);
                    discountEnded = PlatformSubscriptionEvent.Record(
                        this,
                        PlatformSubscriptionEventCodes.DiscountEnded,
                        from,
                        null,
                        utcNow,
                        arguments: new Dictionary<string, string>(StringComparer.Ordinal) { ["endReason"] = nameof(PlatformDiscountEndReason.Completed) });
                }
            }
        }

        MarkAsModified(utcNow);

        PlatformPlanVersion version = plan.Version(PlanVersionId);
        decimal amount = PlatformPricing.PeriodAmount(version, Term, Licences);
        decimal off = CurrentPeriodOff(amount);
        Dictionary<string, string> arguments = Describe(plan.Code, version.Number, Term, Licences);
        arguments["periodEnd"] = Stamp(PeriodEndUtc!.Value);
        arguments["amount"] = (amount - off).ToString(CultureInfo.InvariantCulture);

        return new PlatformSubscriptionRenewal(
            PlatformSubscriptionEvent.Record(this, PlatformSubscriptionEventCodes.Renewed, from, null, utcNow, arguments: arguments),
            false,
            PlanVersionId,
            PeriodStartUtc,
            PeriodEndUtc,
            amount,
            version.CurrencyCode,
            off,
            discountEnded);
    }

    // =====================================================
    // METHOD: CANCELLATION
    // =====================================================
    public PlatformSubscriptionEvent RequestCancellation(string? reason, Guid? actorUserId, DateTimeOffset utcNow)
    {
        string why = PlatformSubscriptionText.Reason(reason);
        RequireNotEnded();

        if (Status is PlatformSubscriptionStatus.Trialing or PlatformSubscriptionStatus.PendingPayment)
        {
            return End(PlatformSubscriptionEndReason.Cancelled, utcNow, actorUserId, why, utcNow);
        }

        if (Status == PlatformSubscriptionStatus.Suspended)
        {
            throw new DomainException(PlatformSubscriptionErrors.NotAllowedInStatus(Status));
        }

        if (CancelAtPeriodEnd)
        {
            throw new DomainException(PlatformSubscriptionErrors.CancellationAlreadyRequested);
        }

        CancelAtPeriodEnd = true;
        CancellationRequestedUtc = utcNow;
        ScheduledChange = null;
        MarkAsModified(utcNow);

        return PlatformSubscriptionEvent.Record(
            this,
            PlatformSubscriptionEventCodes.CancellationRequested,
            Status,
            actorUserId,
            utcNow,
            why,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["effective"] = Stamp(PeriodEndUtc!.Value) });
    }

    public PlatformSubscriptionEvent Resume(Guid? actorUserId, DateTimeOffset utcNow)
    {
        RequireNotEnded();

        if (!CancelAtPeriodEnd)
        {
            throw new DomainException(PlatformSubscriptionErrors.NoCancellationRequested);
        }

        CancelAtPeriodEnd = false;
        CancellationRequestedUtc = null;
        MarkAsModified(utcNow);

        return PlatformSubscriptionEvent.Record(this, PlatformSubscriptionEventCodes.Resumed, Status, actorUserId, utcNow);
    }

    public PlatformSubscriptionEvent EndNow(string? reason, Guid actorUserId, DateTimeOffset utcNow)
    {
        string why = PlatformSubscriptionText.Reason(reason);
        Guid actor = PlatformSubscriptionText.Actor(actorUserId);
        RequireNotEnded();

        return End(PlatformSubscriptionEndReason.EndedByStaff, utcNow, actor, why, utcNow);
    }

    // =====================================================
    // METHOD: TENANT STATUS (D-S10)
    // =====================================================
    public PlatformSubscriptionEvent? SuspendWithTenant(DateTimeOffset utcNow)
    {
        if (Status is PlatformSubscriptionStatus.Ended or PlatformSubscriptionStatus.Suspended)
        {
            return null;
        }

        PlatformSubscriptionStatus from = Status;
        StatusBeforeSuspension = from;
        MoveTo(PlatformSubscriptionStatus.Suspended, utcNow);

        return PlatformSubscriptionEvent.Record(this, PlatformSubscriptionEventCodes.SuspendedWithTenant, from, null, utcNow);
    }

    public PlatformSubscriptionEvent? RestoreWithTenant(DateTimeOffset utcNow)
    {
        if (Status != PlatformSubscriptionStatus.Suspended)
        {
            return null;
        }

        PlatformSubscriptionStatus target = StatusBeforeSuspension ?? PlatformSubscriptionStatus.Active;
        StatusBeforeSuspension = null;
        MoveTo(target, utcNow);

        return PlatformSubscriptionEvent.Record(this, PlatformSubscriptionEventCodes.RestoredWithTenant, PlatformSubscriptionStatus.Suspended, null, utcNow);
    }

    public PlatformSubscriptionEvent? EndWithTenant(DateTimeOffset utcNow)
    {
        return Status == PlatformSubscriptionStatus.Ended
            ? null
            : End(PlatformSubscriptionEndReason.TenantClosed, utcNow, null, null, utcNow);
    }

    // =====================================================
    // METHOD: DISCOUNTS (D-S18 to D-S24)
    // =====================================================
    public PlatformSubscriptionEvent RequestDiscount(
        PlatformDiscountTerms terms,
        PlatformDiscountSource source,
        Guid? sourceId,
        string? reason,
        bool needsApproval,
        Guid? actorUserId,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(terms);
        string why = PlatformSubscriptionText.Reason(reason);
        RequireChangeable();

        if (PendingDiscount is not null)
        {
            throw new DomainException(PlatformDiscountErrors.AlreadyPending);
        }

        PlatformSubscriptionDiscount discount = new(Id, terms, source, sourceId, why, actorUserId, needsApproval, utcNow);
        _discounts.Add(discount);
        Dictionary<string, string> arguments = terms.Describe();
        arguments["source"] = source.ToString();

        if (needsApproval)
        {
            MarkAsModified(utcNow);

            return PlatformSubscriptionEvent.Record(this, PlatformSubscriptionEventCodes.DiscountRequested, Status, actorUserId, utcNow, why, arguments);
        }

        Activate(discount, utcNow);

        return PlatformSubscriptionEvent.Record(this, PlatformSubscriptionEventCodes.DiscountApplied, Status, actorUserId, utcNow, why, arguments);
    }

    public PlatformSubscriptionEvent ApproveDiscount(Guid discountId, string? reason, Guid actorUserId, DateTimeOffset utcNow)
    {
        Guid actor = PlatformSubscriptionText.Actor(actorUserId);
        RequireChangeable();
        PlatformSubscriptionDiscount discount = Discount(discountId);
        string? why = string.IsNullOrWhiteSpace(reason) ? null : PlatformSubscriptionText.Reason(reason);
        discount.Approve(actor, why, utcNow);
        Activate(discount, utcNow);

        return PlatformSubscriptionEvent.Record(this, PlatformSubscriptionEventCodes.DiscountApproved, Status, actor, utcNow, why, discount.Terms.Describe());
    }

    public PlatformSubscriptionEvent RejectDiscount(Guid discountId, string? reason, Guid actorUserId, DateTimeOffset utcNow)
    {
        Guid actor = PlatformSubscriptionText.Actor(actorUserId);
        string why = PlatformSubscriptionText.Reason(reason);
        PlatformSubscriptionDiscount discount = Discount(discountId);
        discount.Reject(actor, why, utcNow);
        MarkAsModified(utcNow);

        return PlatformSubscriptionEvent.Record(this, PlatformSubscriptionEventCodes.DiscountRejected, Status, actor, utcNow, why, discount.Terms.Describe());
    }

    public PlatformSubscriptionEvent EndDiscount(string? reason, Guid actorUserId, DateTimeOffset utcNow)
    {
        Guid actor = PlatformSubscriptionText.Actor(actorUserId);
        string why = PlatformSubscriptionText.Reason(reason);
        PlatformSubscriptionDiscount discount = ActiveDiscount ?? throw new DomainException(PlatformDiscountErrors.NoActiveDiscount);
        discount.End(PlatformDiscountEndReason.EndedByStaff, utcNow);
        MarkAsModified(utcNow);

        return PlatformSubscriptionEvent.Record(
            this,
            PlatformSubscriptionEventCodes.DiscountEnded,
            Status,
            actor,
            utcNow,
            why,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["endReason"] = nameof(PlatformDiscountEndReason.EndedByStaff) });
    }

    // The discount on the period in progress (0 when none applied to it).
    public decimal CurrentPeriodOff(decimal periodAmount)
    {
        return ActiveDiscount is { } discount && discount.AppliedToPeriod(PeriodStartUtc)
            ? discount.Terms.Off(periodAmount)
            : 0m;
    }

    // The discount on the next period: the first paid period while in a trial
    // or awaiting payment, otherwise the period after the current one.
    public decimal NextPeriodOff(decimal periodAmount, DateTimeOffset utcNow)
    {
        DateTimeOffset start = PeriodEndUtc ?? utcNow;

        return ActiveDiscount is { } discount && discount.AppliesToPeriodStarting(start)
            ? discount.Terms.Off(periodAmount)
            : 0m;
    }

    // =====================================================
    // METHOD: ONE-TIME CHARGES AND SETUP FEE (D-S23)
    // =====================================================
    public decimal SetupFeeFor(PlatformPlanVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        return SetupFeeOverride ?? version.SetupFee;
    }

    public PlatformSubscriptionEvent ChangeSetupFee(decimal? amount, PlatformPlanVersion version, string? reason, Guid actorUserId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(version);
        Guid actor = PlatformSubscriptionText.Actor(actorUserId);
        string why = PlatformSubscriptionText.Reason(reason);
        RequireNotEnded();

        if (SetupFeeCharged)
        {
            throw new DomainException(PlatformDiscountErrors.SetupFeeCharged);
        }

        if (amount is decimal value && (value < 0m || value > PlatformSubscriptionLimits.MaxPrice || decimal.Round(value, PlatformSubscriptionLimits.MoneyScale) != value))
        {
            throw new DomainException(PlatformDiscountErrors.AmountInvalid);
        }

        decimal before = SetupFeeFor(version);
        SetupFeeOverride = amount;
        MarkAsModified(utcNow);

        return PlatformSubscriptionEvent.Record(
            this,
            PlatformSubscriptionEventCodes.SetupFeeChanged,
            Status,
            actor,
            utcNow,
            why,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["from"] = before.ToString(CultureInfo.InvariantCulture),
                ["to"] = SetupFeeFor(version).ToString(CultureInfo.InvariantCulture),
                ["currency"] = version.CurrencyCode
            });
    }

    public PlatformSubscriptionEvent AddCharge(string? description, decimal amount, string currencyCode, string? reason, Guid actorUserId, DateTimeOffset utcNow)
    {
        Guid actor = PlatformSubscriptionText.Actor(actorUserId);
        string why = PlatformSubscriptionText.Reason(reason);
        RequireNotEnded();
        string text = description?.Trim() ?? string.Empty;

        if (text.Length == 0)
        {
            throw new DomainException(PlatformDiscountErrors.ChargeDescriptionRequired);
        }

        if (text.Length > PlatformDiscountTerms.ChargeDescriptionMaxLength)
        {
            throw new DomainException(PlatformDiscountErrors.ChargeDescriptionTooLong);
        }

        if (amount <= 0m || amount > PlatformSubscriptionLimits.MaxPrice || decimal.Round(amount, PlatformSubscriptionLimits.MoneyScale) != amount)
        {
            throw new DomainException(PlatformDiscountErrors.AmountInvalid);
        }

        PlatformOneTimeCharge charge = new(Id, text, amount, currencyCode, why, actor, utcNow);
        _charges.Add(charge);
        MarkAsModified(utcNow);

        return PlatformSubscriptionEvent.Record(
            this,
            PlatformSubscriptionEventCodes.ChargeAdded,
            Status,
            actor,
            utcNow,
            why,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["description"] = text,
                ["amount"] = amount.ToString(CultureInfo.InvariantCulture),
                ["currency"] = currencyCode
            });
    }

    public PlatformSubscriptionEvent CancelCharge(Guid chargeId, string? reason, Guid actorUserId, DateTimeOffset utcNow)
    {
        Guid actor = PlatformSubscriptionText.Actor(actorUserId);
        string why = PlatformSubscriptionText.Reason(reason);
        PlatformOneTimeCharge charge = _charges.FirstOrDefault(item => item.Id == chargeId)
            ?? throw new DomainException(PlatformDiscountErrors.ChargeNotFound);
        charge.Cancel(actor, utcNow);
        MarkAsModified(utcNow);

        return PlatformSubscriptionEvent.Record(
            this,
            PlatformSubscriptionEventCodes.ChargeCancelled,
            Status,
            actor,
            utcNow,
            why,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["description"] = charge.Description,
                ["amount"] = charge.Amount.ToString(CultureInfo.InvariantCulture),
                ["currency"] = charge.CurrencyCode
            });
    }

    // A subscription awaiting payment with nothing to pay (free first period,
    // no setup fee, no pending charge) starts at once: free months need no payment.
    public PlatformSubscriptionEvent? ActivateIfNothingDue(PlatformPlanVersion version, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(version);

        if (Status != PlatformSubscriptionStatus.PendingPayment)
        {
            return null;
        }

        decimal first = PlatformPricing.PeriodAmount(version, Term, Licences);
        bool nothingDue = first - NextPeriodOff(first, utcNow) == 0m
            && (SetupFeeCharged || SetupFeeFor(version) == 0m)
            && !_charges.Any(item => item.Status == PlatformOneTimeChargeStatus.Pending);

        return nothingDue ? Activate(PlatformSubscriptionEventCodes.ActivatedNothingDue, null, null, utcNow) : null;
    }

    // =====================================================
    // METHOD: HELPERS
    // =====================================================
    private void Activate(PlatformSubscriptionDiscount discount, DateTimeOffset utcNow)
    {
        foreach (PlatformSubscriptionDiscount other in _discounts.Where(item => item.IsActive && item.Id != discount.Id).ToArray())
        {
            other.End(PlatformDiscountEndReason.Replaced, utcNow);
        }

        MarkAsModified(utcNow);
    }

    private PlatformSubscriptionDiscount Discount(Guid discountId)
    {
        return _discounts.FirstOrDefault(item => item.Id == discountId)
            ?? throw new DomainException(PlatformDiscountErrors.DiscountNotFound);
    }

    private PlatformSubscriptionEvent End(
        PlatformSubscriptionEndReason endReason,
        DateTimeOffset endedUtc,
        Guid? actorUserId,
        string? reason,
        DateTimeOffset utcNow)
    {
        PlatformSubscriptionStatus from = Status;
        EndedUtc = endedUtc;
        EndReason = endReason;
        ScheduledChange = null;
        CancelAtPeriodEnd = false;
        StatusBeforeSuspension = null;
        StatusBeforeBlock = null;
        BlockedUtc = null;

        foreach (PlatformSubscriptionDiscount discount in _discounts.Where(item => item.IsActive || item.IsPending))
        {
            discount.End(PlatformDiscountEndReason.SubscriptionEnded, utcNow);
        }

        MoveTo(PlatformSubscriptionStatus.Ended, utcNow);

        return PlatformSubscriptionEvent.Record(
            this,
            PlatformSubscriptionEventCodes.Ended,
            from,
            actorUserId,
            utcNow,
            reason,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["endReason"] = endReason.ToString() });
    }

    private void MoveTo(PlatformSubscriptionStatus status, DateTimeOffset utcNow)
    {
        Status = status;
        StatusChangedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    private void RequireTrialing()
    {
        if (Status != PlatformSubscriptionStatus.Trialing)
        {
            throw new DomainException(PlatformSubscriptionErrors.NotTrialing);
        }
    }

    private void RequireNotEnded()
    {
        if (Status == PlatformSubscriptionStatus.Ended)
        {
            throw new DomainException(PlatformSubscriptionErrors.Ended);
        }
    }

    private void RequireChangeable()
    {
        RequireNotEnded();

        if (Status is PlatformSubscriptionStatus.Suspended or PlatformSubscriptionStatus.BlockedForNonPayment)
        {
            throw new DomainException(PlatformSubscriptionErrors.NotAllowedInStatus(Status));
        }
    }

    private static PlatformPlanVersion Chosen(PlatformPlan plan, PlatformPlanVersion? fallback = null)
    {
        if (plan.CanBeChosen)
        {
            return plan.Current!;
        }

        return fallback ?? throw new DomainException(PlatformSubscriptionErrors.PlanNotAvailable);
    }

    private static void CheckTermAndLicences(PlatformPlanVersion version, PlatformSubscriptionTerm term, int licences, int maxLicences, int activeLicences)
    {
        if (!Enum.IsDefined(term))
        {
            throw new DomainException(PlatformSubscriptionErrors.TermInvalid);
        }

        if (!version.Offers(term))
        {
            throw new DomainException(PlatformSubscriptionErrors.TermNotOffered);
        }

        if (licences < 1 || licences > PlatformSubscriptionLimits.MaxLicences)
        {
            throw new DomainException(PlatformSubscriptionErrors.LicencesInvalid);
        }

        if (licences < version.MinimumLicences)
        {
            throw new DomainException(PlatformSubscriptionErrors.BelowMinimum(version.MinimumLicences));
        }

        if (licences > maxLicences)
        {
            throw new DomainException(PlatformSubscriptionErrors.AboveLimit(maxLicences));
        }

        if (licences < activeLicences)
        {
            throw new DomainException(PlatformSubscriptionErrors.BelowActiveLicences(activeLicences));
        }
    }

    private static Dictionary<string, string> Describe(string planCode, int versionNumber, PlatformSubscriptionTerm term, int licences)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["plan"] = planCode,
            ["version"] = versionNumber.ToString(CultureInfo.InvariantCulture),
            ["term"] = term.ToString(),
            ["licences"] = licences.ToString(CultureInfo.InvariantCulture)
        };
    }

    private static string Stamp(DateTimeOffset value)
    {
        return value.ToString("O", CultureInfo.InvariantCulture);
    }
}
