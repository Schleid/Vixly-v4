namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformRoleDetail.cs
//
// PURPOSE:
// One role as the role page shows it.
//
// RESPONSIBILITIES:
// - Carry the role, its permission codes and its current holders
//
// IMPORTANT NOTES:
// - Holders are grants in force now, sorted by name
// =========================================================
public sealed class PlatformRoleDetail
{
    public Guid PlatformRoleId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? SystemKey { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedUtc { get; init; }

    public IReadOnlyList<string> PermissionCodes { get; init; } = Array.Empty<string>();

    public IReadOnlyList<PlatformRoleHolderItem> Holders { get; init; } = Array.Empty<PlatformRoleHolderItem>();
}
