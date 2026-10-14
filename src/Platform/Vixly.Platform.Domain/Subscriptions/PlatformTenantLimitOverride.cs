using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformTenantLimitOverride.cs
//
// PURPOSE:
// A staff override of one of a tenant's plan limits (D-S8).
//
// RESPONSIBILITIES:
// - Set a value for one limit, with a reason, an author and an optional end
// - Be removed with an author; say whether it applies at a given time
//
// IMPORTANT NOTES:
// - Aggregate root. Rows are kept after removal: they are the override history
// - At most one open override per tenant and limit: the use case closes the old one, a filtered unique index guarantees
// =========================================================
public sealed class PlatformTenantLimitOverride : AuditableEntity
{
    private PlatformTenantLimitOverride()
    {
        Reason = string.Empty;
    }

    private PlatformTenantLimitOverride(
        Guid platformTenantId,
        PlatformPlanLimitKind kind,
        int value,
        string reason,
        DateTimeOffset? endsUtc,
        Guid setByUserId,
        DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        PlatformTenantId = platformTenantId;
        Kind = kind;
        Value = value;
        Reason = reason;
        EndsUtc = endsUtc;
        SetByUserId = setByUserId;
    }

    public Guid PlatformTenantId { get; private set; }

    public PlatformPlanLimitKind Kind { get; private set; }

    public int Value { get; private set; }

    public string Reason { get; private set; }

    public DateTimeOffset? EndsUtc { get; private set; }

    public Guid SetByUserId { get; private set; }

    public DateTimeOffset? RemovedUtc { get; private set; }

    public Guid? RemovedByUserId { get; private set; }

    public bool IsOpen => RemovedUtc is null;

    public static PlatformTenantLimitOverride Set(
        Guid platformTenantId,
        PlatformPlanLimitKind kind,
        int value,
        string? reason,
        DateTimeOffset? endsUtc,
        Guid actorUserId,
        DateTimeOffset utcNow)
    {
        if (platformTenantId == Guid.Empty)
        {
            throw new DomainException(PlatformSubscriptionErrors.TenantRequired);
        }

        if (!Enum.IsDefined(kind))
        {
            throw new DomainException(PlatformSubscriptionErrors.OverrideLimitInvalid);
        }

        if (value < 0 || value > PlatformSubscriptionLimits.MaxLimitValue)
        {
            throw new DomainException(PlatformSubscriptionErrors.OverrideValueInvalid);
        }

        if (endsUtc is { } end && (end <= utcNow || end.Offset != TimeSpan.Zero))
        {
            throw new DomainException(PlatformSubscriptionErrors.OverrideEndInvalid);
        }

        return new PlatformTenantLimitOverride(
            platformTenantId,
            kind,
            value,
            PlatformSubscriptionText.Reason(reason),
            endsUtc,
            PlatformSubscriptionText.Actor(actorUserId),
            utcNow);
    }

    public bool AppliesAt(DateTimeOffset utcNow)
    {
        return IsOpen && (EndsUtc is null || EndsUtc > utcNow);
    }

    public void Remove(Guid actorUserId, DateTimeOffset utcNow)
    {
        Guid actor = PlatformSubscriptionText.Actor(actorUserId);

        if (!IsOpen)
        {
            throw new DomainException(PlatformSubscriptionErrors.OverrideClosed);
        }

        RemovedUtc = utcNow;
        RemovedByUserId = actor;
        MarkAsModified(utcNow);
    }
}
