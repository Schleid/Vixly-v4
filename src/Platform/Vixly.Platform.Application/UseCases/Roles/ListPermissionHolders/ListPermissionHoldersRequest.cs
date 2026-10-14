namespace Vixly.Platform.Application.UseCases.Roles.ListPermissionHolders;

// =========================================================
// FILE: ListPermissionHoldersRequest.cs
//
// PURPOSE:
// Input for ListPermissionHolders.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The actor comes from the authorization context (S-2)
// =========================================================
public sealed class ListPermissionHoldersRequest
{
    public string PermissionCode { get; init; } = string.Empty;
}
