namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformStaffOptionItem.cs
//
// PURPOSE:
// An active staff member offered as account manager.
//
// RESPONSIBILITIES:
// - Carry read-only data to the console
//
// IMPORTANT NOTES:
// - Name only: no email or role is exposed here
// =========================================================
public sealed record PlatformStaffOptionItem(
    Guid PlatformUserId,
    string Name);
