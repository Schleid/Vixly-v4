using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformSubscriptionDiscount.cs
//
// PURPOSE:
// One discount on a subscription, from request to end (D-S18 to D-S24).
//
// RESPONSIBILITIES:
// - Hold the terms, where they came from, who granted and who approved them
// - Count the billing periods it was used for
// - Know whether it still applies to a given period
//
// IMPORTANT NOTES:
// - Owned by the PlatformSubscription aggregate; changed only through it
// - Kept as history once rejected or ended
// - Applies to periods that start while it is active, so a grant mid-period starts at the next period
// =========================================================
public sealed class PlatformSubscriptionDiscount : AuditableEntity
{
    private PlatformSubscriptionDiscount()
    {
        Terms = null!;
        Reason = string.Empty;
    }

    internal PlatformSubscriptionDiscount(
        Guid platformSubscriptionId,
        PlatformDiscountTerms terms,
        PlatformDiscountSource source,
        Guid? sourceId,
        string reason,
        Guid? grantedByUserId,
        bool needsApproval,
        DateTimeOffset utcNow)
        : base(utcNow)
    {
        PlatformSubscriptionId = platformSubscriptionId;
        Terms = terms;
        Source = source;
        SourceId = sourceId;
        Reason = reason;
        GrantedByUserId = grantedByUserId;
        Status = needsApproval ? PlatformDiscountStatus.PendingApproval : PlatformDiscountStatus.Active;
    }

    public Guid PlatformSubscriptionId { get; private set; }

    public PlatformDiscountTerms Terms { get; private set; }

    public PlatformDiscountSource Source { get; private set; }

    public Guid? SourceId { get; private set; }

    public string Reason { get; private set; }

    public Guid? GrantedByUserId { get; private set; }

    public PlatformDiscountStatus Status { get; private set; }

    public Guid? DecidedByUserId { get; private set; }

    public DateTimeOffset? DecidedUtc { get; private set; }

    public string? DecisionReason { get; private set; }

    public int PeriodsUsed { get; private set; }

    public DateTimeOffset? LastPeriodStartUtc { get; private set; }

    public DateTimeOffset? EndedUtc { get; private set; }

    public PlatformDiscountEndReason? EndReason { get; private set; }

    public DateTimeOffset? EndNoticeSentUtc { get; private set; }

    public bool IsActive => Status == PlatformDiscountStatus.Active;

    public bool IsPending => Status == PlatformDiscountStatus.PendingApproval;

    // Whether a period starting at periodStartUtc would still get it.
    public bool AppliesToPeriodStarting(DateTimeOffset periodStartUtc)
    {
        return IsActive && Terms.Duration switch
        {
            PlatformDiscountDuration.Periods => PeriodsUsed < Terms.Periods,
            PlatformDiscountDuration.UntilDate => periodStartUtc < Terms.EndsUtc,
            _ => true
        };
    }

    public bool AppliedToPeriod(DateTimeOffset? periodStartUtc)
    {
        return IsActive && periodStartUtc is not null && LastPeriodStartUtc == periodStartUtc;
    }

    // The last moment the discount still covers, when it is known.
    public DateTimeOffset? LastsUntil(DateTimeOffset? currentPeriodEndUtc, PlatformSubscriptionTerm term)
    {
        return Terms.Duration switch
        {
            PlatformDiscountDuration.UntilDate => Terms.EndsUtc,
            PlatformDiscountDuration.Periods when currentPeriodEndUtc is DateTimeOffset end && LastPeriodStartUtc is not null =>
                AddPeriods(end, Terms.Periods!.Value - PeriodsUsed, term),
            _ => null
        };
    }

    internal void UsePeriod(DateTimeOffset periodStartUtc, DateTimeOffset utcNow)
    {
        PeriodsUsed++;
        LastPeriodStartUtc = periodStartUtc;
        MarkAsModified(utcNow);
    }

    internal void Approve(Guid actorUserId, string? reason, DateTimeOffset utcNow)
    {
        Decide(PlatformDiscountStatus.Active, actorUserId, reason, utcNow);
    }

    internal void Reject(Guid actorUserId, string reason, DateTimeOffset utcNow)
    {
        Decide(PlatformDiscountStatus.Rejected, actorUserId, reason, utcNow);
    }

    internal void End(PlatformDiscountEndReason reason, DateTimeOffset utcNow)
    {
        Status = PlatformDiscountStatus.Ended;
        EndReason = reason;
        EndedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    public void RecordEndNotice(DateTimeOffset utcNow)
    {
        EndNoticeSentUtc = utcNow;
        MarkAsModified(utcNow);
    }

    private void Decide(PlatformDiscountStatus status, Guid actorUserId, string? reason, DateTimeOffset utcNow)
    {
        if (!IsPending)
        {
            throw new DomainException(PlatformDiscountErrors.NotPending);
        }

        if (GrantedByUserId == actorUserId)
        {
            throw new DomainException(PlatformDiscountErrors.OwnRequest);
        }

        Status = status;
        DecidedByUserId = actorUserId;
        DecidedUtc = utcNow;
        DecisionReason = reason;
        MarkAsModified(utcNow);
    }

    private static DateTimeOffset AddPeriods(DateTimeOffset start, int periods, PlatformSubscriptionTerm term)
    {
        return term == PlatformSubscriptionTerm.Annual ? start.AddYears(periods) : start.AddMonths(periods);
    }
}
