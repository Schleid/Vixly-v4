using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformDepartment.cs
//
// PURPOSE:
// An organisational group of Vixly staff (for example
// Billing, Support).
//
// RESPONSIBILITIES:
// - Hold the name, description and active state
//
// IMPORTANT NOTES:
// - Aggregate root
// - A label only: it grants no permission (decision D-S3).
//   V3 stored a department scope on roles but never enforced
//   it; V4 does not carry that over
// =========================================================
public sealed class PlatformDepartment : AuditableEntity
{
    private PlatformDepartment()
    {
        Name = string.Empty;
    }

    private PlatformDepartment(string name, string? description, DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        Name = name;
        Description = description;
        IsActive = true;
    }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    // =====================================================
    // METHOD: CREATE
    // =====================================================
    public static PlatformDepartment Create(string name, string? description, DateTimeOffset createdUtc)
    {
        return new PlatformDepartment(
            PlatformIdentityText.RequiredName(name),
            PlatformIdentityText.OptionalDescription(description),
            createdUtc);
    }

    // =====================================================
    // METHOD: CHANGE
    // =====================================================
    public void Update(string name, string? description, DateTimeOffset utcNow)
    {
        Name = PlatformIdentityText.RequiredName(name);
        Description = PlatformIdentityText.OptionalDescription(description);
        MarkAsModified(utcNow);
    }

    public void Deactivate(DateTimeOffset utcNow)
    {
        if (!IsActive)
        {
            throw new DomainException(PlatformIdentityErrors.DepartmentAlreadyInactive);
        }

        IsActive = false;
        MarkAsModified(utcNow);
    }

    public void Reactivate(DateTimeOffset utcNow)
    {
        if (IsActive)
        {
            throw new DomainException(PlatformIdentityErrors.DepartmentAlreadyActive);
        }

        IsActive = true;
        MarkAsModified(utcNow);
    }
}
