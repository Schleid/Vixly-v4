namespace Vixly.Shared.UI.Primitives;

// =========================================================
// FILE: VxMessageType.cs
//
// PURPOSE:
// Message severity categories for alert and notice primitives.
//
// RESPONSIBILITIES:
// - Define alert severity levels mapped to semantic tokens
//
// IMPORTANT NOTES:
// - Screen reader roles (alert vs status) depend on severity
// =========================================================
public enum VxMessageType
{
    Info = 0,
    Success = 1,
    Warning = 2,
    Error = 3
}
