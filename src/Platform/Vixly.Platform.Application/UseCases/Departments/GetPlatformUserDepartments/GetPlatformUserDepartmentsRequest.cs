namespace Vixly.Platform.Application.UseCases.Departments.GetPlatformUserDepartments;

// =========================================================
// FILE: GetPlatformUserDepartmentsRequest.cs
//
// PURPOSE:
// Input for GetPlatformUserDepartments.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The actor comes from the authorization context (S-2)
// =========================================================
public sealed class GetPlatformUserDepartmentsRequest
{
    public Guid PlatformUserId { get; init; }
}
