using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformTenantNote.cs
//
// PURPOSE:
// An internal note about a tenant, written by staff (D-T16).
//
// RESPONSIBILITIES:
// - Validate and hold the text, the author and the time
//
// IMPORTANT NOTES:
// - Aggregate root of its own, so a tenant never loads its notes
// - Append-only: a correction is a new note
// - Never sent to the tenant or to another plane
// =========================================================
public sealed class PlatformTenantNote : AuditableEntity
{
    private PlatformTenantNote()
    {
        Body = string.Empty;
    }

    private PlatformTenantNote(Guid platformTenantId, string body, Guid authorUserId, DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        PlatformTenantId = platformTenantId;
        Body = body;
        AuthorUserId = authorUserId;
    }

    public Guid PlatformTenantId { get; private set; }

    public string Body { get; private set; }

    public Guid AuthorUserId { get; private set; }

    // =====================================================
    // METHOD: WRITE
    // =====================================================
    public static PlatformTenantNote Write(Guid platformTenantId, string? body, Guid authorUserId, DateTimeOffset utcNow)
    {
        if (platformTenantId == Guid.Empty)
        {
            throw new DomainException(PlatformTenantErrors.TenantRequired);
        }

        return new PlatformTenantNote(
            platformTenantId,
            PlatformTenantText.Required(
                body,
                PlatformTenantLimits.NoteMaxLength,
                PlatformTenantErrors.NoteRequired,
                PlatformTenantErrors.NoteTooLong),
            PlatformTenantText.Actor(authorUserId),
            utcNow);
    }
}
