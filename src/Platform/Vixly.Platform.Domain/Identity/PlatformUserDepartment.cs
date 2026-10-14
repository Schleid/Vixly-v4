using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformUserDepartment.cs
//
// PURPOSE:
// Records that a staff user belongs to a department.
//
// RESPONSIBILITIES:
// - Link the user to the department
// - Mark the user's primary department
//
// IMPORTANT NOTES:
// - Part of the PlatformUser aggregate
// - Departments are labels only (decision D-S3); membership
//   grants no permission
// =========================================================
public sealed class PlatformUserDepartment : AuditableEntity
{
    private PlatformUserDepartment()
    {
    }

    internal PlatformUserDepartment(
        Guid platformUserId,
        Guid platformDepartmentId,
        bool isPrimary,
        DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        PlatformUserId = platformUserId;
        PlatformDepartmentId = platformDepartmentId;
        IsPrimary = isPrimary;
    }

    public Guid PlatformUserId { get; private set; }

    public Guid PlatformDepartmentId { get; private set; }

    public bool IsPrimary { get; private set; }

    internal void SetPrimary(bool isPrimary, DateTimeOffset utcNow)
    {
        if (IsPrimary == isPrimary)
        {
            return;
        }

        IsPrimary = isPrimary;
        MarkAsModified(utcNow);
    }
}
