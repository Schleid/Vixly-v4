using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformPortalSlug.cs
//
// PURPOSE:
// One claimed portal slug: pending for a registration, or the
// permanent address of a tenant (D-T10, D-T11).
//
// RESPONSIBILITIES:
// - Claim a slug for a registration (held for a time) or a tenant
// - Make a pending claim permanent when the tenant is created
// - Let a lapsed pending claim be taken by someone else
//
// IMPORTANT NOTES:
// - Aggregate root. The table has a unique index on Slug: two
//   claims racing for one slug cannot both commit
// - A permanent claim is never released, so an address is never
//   reused. The only removal is a correction before the tenant
//   has ever been active, done by the use case
// =========================================================
public sealed class PlatformPortalSlug : AuditableEntity
{
    private PlatformPortalSlug()
    {
        Slug = string.Empty;
    }

    private PlatformPortalSlug(string slug, DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        Slug = slug;
    }

    public string Slug { get; private set; }

    public PlatformPortalSlugState State { get; private set; }

    public Guid? PlatformTenantId { get; private set; }

    public Guid? PlatformTenantRegistrationId { get; private set; }

    public DateTimeOffset? HeldUntilUtc { get; private set; }

    public bool IsLapsed(DateTimeOffset utcNow)
    {
        return State == PlatformPortalSlugState.Pending && HeldUntilUtc <= utcNow;
    }

    // =====================================================
    // METHOD: CLAIM
    // =====================================================
    public static PlatformPortalSlug ClaimForRegistration(
        string slug,
        Guid registrationId,
        DateTimeOffset holdUntilUtc,
        DateTimeOffset utcNow)
    {
        PlatformPortalSlug claim = new(PlatformPortalSlugFormat.Normalize(slug), utcNow);
        claim.HoldFor(registrationId, holdUntilUtc, utcNow);

        return claim;
    }

    public static PlatformPortalSlug ClaimForTenant(string slug, Guid tenantId, DateTimeOffset utcNow)
    {
        PlatformPortalSlug claim = new(PlatformPortalSlugFormat.Normalize(slug), utcNow);
        claim.Settle(tenantId, utcNow);

        return claim;
    }

    public void ReclaimForRegistration(Guid registrationId, DateTimeOffset holdUntilUtc, DateTimeOffset utcNow)
    {
        EnsureLapsed(utcNow);
        HoldFor(registrationId, holdUntilUtc, utcNow);
    }

    public void ReclaimForTenant(Guid tenantId, DateTimeOffset utcNow)
    {
        EnsureLapsed(utcNow);
        Settle(tenantId, utcNow);
    }

    // =====================================================
    // METHOD: HOLD AND SETTLE
    // =====================================================
    public void ExtendHold(Guid registrationId, DateTimeOffset holdUntilUtc, DateTimeOffset utcNow)
    {
        EnsureHeldBy(registrationId);

        if (holdUntilUtc > HeldUntilUtc)
        {
            HeldUntilUtc = holdUntilUtc;
            MarkAsModified(utcNow);
        }
    }

    public void MakePermanent(Guid registrationId, Guid tenantId, DateTimeOffset utcNow)
    {
        EnsureHeldBy(registrationId);
        Settle(tenantId, utcNow);
    }

    public bool IsHeldBy(Guid registrationId)
    {
        return State == PlatformPortalSlugState.Pending && PlatformTenantRegistrationId == registrationId;
    }

    private void HoldFor(Guid registrationId, DateTimeOffset holdUntilUtc, DateTimeOffset utcNow)
    {
        if (registrationId == Guid.Empty)
        {
            throw new DomainException(PlatformRegistrationErrors.NotFound);
        }

        State = PlatformPortalSlugState.Pending;
        PlatformTenantRegistrationId = registrationId;
        PlatformTenantId = null;
        HeldUntilUtc = holdUntilUtc;
        MarkAsModified(utcNow);
    }

    private void Settle(Guid tenantId, DateTimeOffset utcNow)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException(PlatformTenantErrors.TenantRequired);
        }

        State = PlatformPortalSlugState.Permanent;
        PlatformTenantId = tenantId;
        HeldUntilUtc = null;
        MarkAsModified(utcNow);
    }

    private void EnsureLapsed(DateTimeOffset utcNow)
    {
        if (!IsLapsed(utcNow))
        {
            throw new DomainException(PlatformTenantErrors.SlugTaken);
        }
    }

    private void EnsureHeldBy(Guid registrationId)
    {
        if (!IsHeldBy(registrationId))
        {
            throw new DomainException(PlatformTenantErrors.SlugTaken);
        }
    }
}
