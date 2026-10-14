namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformSubscriptionScheduledChange.cs
//
// PURPOSE:
// A change that applies at the end of the current period (D-S4, D-S5).
//
// RESPONSIBILITIES:
// - Hold the target plan, version, term and licence quantity and when they apply
//
// IMPORTANT NOTES:
// - Owned by PlatformSubscription; at most one at a time
// - At renewal the target plan's current version is used if a newer one was published (owner, D-S2)
// =========================================================
public sealed class PlatformSubscriptionScheduledChange
{
    private PlatformSubscriptionScheduledChange()
    {
    }

    internal PlatformSubscriptionScheduledChange(
        Guid targetPlanId,
        Guid targetVersionId,
        PlatformSubscriptionTerm targetTerm,
        int targetLicences,
        DateTimeOffset effectiveUtc,
        Guid? requestedByUserId,
        DateTimeOffset requestedUtc)
    {
        TargetPlanId = targetPlanId;
        TargetVersionId = targetVersionId;
        TargetTerm = targetTerm;
        TargetLicences = targetLicences;
        EffectiveUtc = effectiveUtc;
        RequestedByUserId = requestedByUserId;
        RequestedUtc = requestedUtc;
    }

    public Guid TargetPlanId { get; private set; }

    public Guid TargetVersionId { get; private set; }

    public PlatformSubscriptionTerm TargetTerm { get; private set; }

    public int TargetLicences { get; private set; }

    public DateTimeOffset EffectiveUtc { get; private set; }

    public Guid? RequestedByUserId { get; private set; }

    public DateTimeOffset RequestedUtc { get; private set; }
}
