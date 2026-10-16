namespace Vixly.Platform.Domain.Payments;

// =========================================================
// FILE: PlatformSavedCardStatus.cs
//
// PURPOSE:
// The state of a saved card.
//
// RESPONSIBILITIES:
// - Name the the state of a saved card
//
// IMPORTANT NOTES:
// - Values are stored; never renumber
// =========================================================
public enum PlatformSavedCardStatus
{
    Active = 1,
    Removed = 2
}
