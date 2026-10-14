namespace Vixly.Platform.Application.UseCases.Departments.ReactivatePlatformDepartment;

// =========================================================
// FILE: ReactivatePlatformDepartmentRequest.cs
//
// PURPOSE:
// Input for ReactivatePlatformDepartment.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The actor comes from the authorization context (S-2)
// =========================================================
public sealed class ReactivatePlatformDepartmentRequest
{
    public Guid PlatformDepartmentId { get; init; }
}
