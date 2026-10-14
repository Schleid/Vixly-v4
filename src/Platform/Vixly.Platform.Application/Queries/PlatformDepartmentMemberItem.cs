namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformDepartmentMemberItem.cs
//
// PURPOSE:
// A staff user in a department.
//
// RESPONSIBILITIES:
// - Carry the person and whether this is their primary department
//
// IMPORTANT NOTES:
// - Membership is a label only (decision D-S3)
// =========================================================
public sealed record PlatformDepartmentMemberItem(
    Guid PlatformUserId,
    string FirstName,
    string LastName,
    string Email,
    bool IsPrimary);
