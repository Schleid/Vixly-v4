using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformRoleAssignment.cs
//
// PURPOSE:
// Records that a staff user holds a role, from when, until
// when, and who granted and revoked it.
//
// RESPONSIBILITIES:
// - Hold the grant, its optional expiry and its revocation
// - Answer whether the grant is in force at a given time
//
// IMPORTANT NOTES:
// - Part of the PlatformUser aggregate
// - Never deleted. A revoked or expired grant stays as history
//   (V3 behaviour, kept)
// - An expiry makes a time-boxed grant (upgrade plan)
// =========================================================
public sealed class PlatformRoleAssignment : AuditableEntity
{
    private PlatformRoleAssignment()
    {
    }

    internal PlatformRoleAssignment(
        Guid platformUserId,
        Guid platformRoleId,
        Guid assignedByUserId,
        DateTimeOffset? expiresUtc,
        DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        PlatformUserId = platformUserId;
        PlatformRoleId = platformRoleId;
        AssignedByUserId = assignedByUserId;
        ExpiresUtc = expiresUtc;
    }

    public Guid PlatformUserId { get; private set; }

    public Guid PlatformRoleId { get; private set; }

    public Guid AssignedByUserId { get; private set; }

    public DateTimeOffset? ExpiresUtc { get; private set; }

    public Guid? RevokedByUserId { get; private set; }

    public DateTimeOffset? RevokedUtc { get; private set; }

    // =====================================================
    // METHOD: IS IN FORCE
    // =====================================================
    public bool IsInForce(DateTimeOffset utcNow)
    {
        return RevokedUtc is null && (ExpiresUtc is null || ExpiresUtc > utcNow);
    }

    internal void Revoke(Guid revokedByUserId, DateTimeOffset utcNow)
    {
        RevokedByUserId = revokedByUserId;
        RevokedUtc = utcNow;
        MarkAsModified(utcNow);
    }
}
