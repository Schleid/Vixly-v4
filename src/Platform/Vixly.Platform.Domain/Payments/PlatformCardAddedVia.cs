namespace Vixly.Platform.Domain.Payments;

// =========================================================
// FILE: PlatformCardAddedVia.cs
//
// PURPOSE:
// How a saved card reached Vixly.
//
// RESPONSIBILITIES:
// - Name the how a saved card reached Vixly
//
// IMPORTANT NOTES:
// - Values are stored; never renumber
// =========================================================
public enum PlatformCardAddedVia
{
    PayLink = 1,
    SaveCardLink = 2,
    Registration = 3
}
