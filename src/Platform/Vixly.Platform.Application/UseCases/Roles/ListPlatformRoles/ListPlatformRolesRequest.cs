namespace Vixly.Platform.Application.UseCases.Roles.ListPlatformRoles;

// =========================================================
// FILE: ListPlatformRolesRequest.cs
//
// PURPOSE:
// Input for ListPlatformRoles.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The actor comes from the authorization context (S-2)
// =========================================================
public sealed class ListPlatformRolesRequest
{
    public bool IncludeInactive { get; init; }
}
