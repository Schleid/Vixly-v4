namespace Vixly.Platform.Application.UseCases.Departments.ListPlatformDepartments;

// =========================================================
// FILE: ListPlatformDepartmentsRequest.cs
//
// PURPOSE:
// Input for ListPlatformDepartments.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The actor comes from the authorization context (S-2)
// =========================================================
public sealed class ListPlatformDepartmentsRequest
{
    public bool IncludeInactive { get; init; }
}
