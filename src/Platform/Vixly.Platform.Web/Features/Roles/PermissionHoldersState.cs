using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Roles;

// =========================================================
// FILE: PermissionHoldersState.cs
//
// PURPOSE:
// What the permission holders list shows.
//
// RESPONSIBILITIES:
// - Hold the holders and any error
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record PermissionHoldersState(bool Loaded, IReadOnlyList<PlatformPermissionHolderItem> Holders, Error? Error)
{
    public static PermissionHoldersState Initial { get; } = new(false, Array.Empty<PlatformPermissionHolderItem>(), null);
}
