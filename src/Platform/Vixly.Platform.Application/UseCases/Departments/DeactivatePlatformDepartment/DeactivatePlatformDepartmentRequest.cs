namespace Vixly.Platform.Application.UseCases.Departments.DeactivatePlatformDepartment;

// =========================================================
// FILE: DeactivatePlatformDepartmentRequest.cs
//
// PURPOSE:
// Input for DeactivatePlatformDepartment.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The actor comes from the authorization context (S-2)
// =========================================================
public sealed class DeactivatePlatformDepartmentRequest
{
    public Guid PlatformDepartmentId { get; init; }
}
