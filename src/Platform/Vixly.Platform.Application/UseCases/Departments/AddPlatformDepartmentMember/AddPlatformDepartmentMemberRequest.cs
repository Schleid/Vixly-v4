namespace Vixly.Platform.Application.UseCases.Departments.AddPlatformDepartmentMember;

// =========================================================
// FILE: AddPlatformDepartmentMemberRequest.cs
//
// PURPOSE:
// Input for AddPlatformDepartmentMember.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The actor comes from the authorization context (S-2)
// =========================================================
public sealed class AddPlatformDepartmentMemberRequest
{
    public Guid PlatformUserId { get; init; }

    public Guid PlatformDepartmentId { get; init; }
}
