namespace Vixly.Platform.Application.UseCases.Departments.UpdatePlatformDepartment;

// =========================================================
// FILE: UpdatePlatformDepartmentRequest.cs
//
// PURPOSE:
// Input for UpdatePlatformDepartment.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The actor comes from the authorization context (S-2)
// =========================================================
public sealed class UpdatePlatformDepartmentRequest
{
    public Guid PlatformDepartmentId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}
