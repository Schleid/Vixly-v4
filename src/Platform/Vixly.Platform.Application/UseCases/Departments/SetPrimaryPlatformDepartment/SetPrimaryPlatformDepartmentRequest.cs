namespace Vixly.Platform.Application.UseCases.Departments.SetPrimaryPlatformDepartment;

// =========================================================
// FILE: SetPrimaryPlatformDepartmentRequest.cs
//
// PURPOSE:
// Input for SetPrimaryPlatformDepartment.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The actor comes from the authorization context (S-2)
// =========================================================
public sealed class SetPrimaryPlatformDepartmentRequest
{
    public Guid PlatformUserId { get; init; }

    public Guid PlatformDepartmentId { get; init; }
}
