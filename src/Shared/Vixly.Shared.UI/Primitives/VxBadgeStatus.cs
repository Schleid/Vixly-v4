namespace Vixly.Shared.UI.Primitives;

// =========================================================
// FILE: VxBadgeStatus.cs
//
// PURPOSE:
// Semantic status categories for status badge primitives.
//
// RESPONSIBILITIES:
// - Define statuses mapped to semantic tokens
//
// IMPORTANT NOTES:
// - Status is never shown by color alone (WCAG 2.2 AA)
// =========================================================
public enum VxBadgeStatus
{
    Active = 0,
    Invited = 1,
    Suspended = 2,
    Locked = 3,
    Deactivated = 4,
    Info = 5,
    Warning = 6,
    Danger = 7,
    Success = 8
}
