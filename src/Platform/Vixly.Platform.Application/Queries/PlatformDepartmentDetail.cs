namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformDepartmentDetail.cs
//
// PURPOSE:
// One department as the department page shows it.
//
// RESPONSIBILITIES:
// - Carry the department and its members
//
// IMPORTANT NOTES:
// - Members sorted by name
// =========================================================
public sealed class PlatformDepartmentDetail
{
    public Guid PlatformDepartmentId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedUtc { get; init; }

    public IReadOnlyList<PlatformDepartmentMemberItem> Members { get; init; } = Array.Empty<PlatformDepartmentMemberItem>();
}
