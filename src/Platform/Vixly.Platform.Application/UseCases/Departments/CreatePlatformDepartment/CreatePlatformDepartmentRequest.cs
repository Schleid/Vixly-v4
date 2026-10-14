namespace Vixly.Platform.Application.UseCases.Departments.CreatePlatformDepartment;

// =========================================================
// FILE: CreatePlatformDepartmentRequest.cs
//
// PURPOSE:
// Input for CreatePlatformDepartment.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The actor comes from the authorization context (S-2)
// =========================================================
public sealed class CreatePlatformDepartmentRequest
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}
