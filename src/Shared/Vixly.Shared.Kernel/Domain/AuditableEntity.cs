using Vixly.Shared.Kernel.Errors;

namespace Vixly.Shared.Kernel.Domain;

// =========================================================
// FILE: AuditableEntity.cs
//
// PURPOSE:
// Base class for persisted domain objects that need an
// identity and audit timestamps.
//
// RESPONSIBILITIES:
// - Provide a sortable primary key
// - Record when the entity was created
// - Record when the entity was last modified
//
// IMPORTANT NOTES:
// - Time is always supplied by the caller (from TimeProvider),
//   never read from the system clock here, so behavior is
//   testable
// - Ids are version 7 GUIDs derived from the creation time, so
//   clustered indexes grow append-only (Global Standard P-6)
// - The parameterless constructor exists only for ORM
//   materialization; domain code uses the other constructor
// - Concrete entities are named with their platform prefix
//   (Global Standard 4.1), for example PlatformInvoice
// =========================================================
public abstract class AuditableEntity
{
    // =====================================================
    // METHOD: ORM CONSTRUCTOR
    // =====================================================
    protected AuditableEntity()
    {
    }

    protected AuditableEntity(DateTimeOffset createdUtc)
    {
        if (createdUtc.Offset != TimeSpan.Zero)
        {
            throw new DomainException(KernelErrors.TimeMustBeUtc);
        }

        Id = Guid.CreateVersion7(createdUtc);
        CreatedUtc = createdUtc;
    }

    public Guid Id { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public DateTimeOffset? LastModifiedUtc { get; private set; }

    // =====================================================
    // METHOD: MARK AS MODIFIED
    // =====================================================
    public void MarkAsModified(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(KernelErrors.TimeMustBeUtc);
        }

        LastModifiedUtc = utcNow;
    }
}
