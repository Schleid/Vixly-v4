namespace Vixly.Platform.Domain.Payments;

// =========================================================
// FILE: PlatformCardProvider.cs
//
// PURPOSE:
// Which card provider Vixly uses (D-P1).
//
// RESPONSIBILITIES:
// - Name the which card provider Vixly uses (D-P1)
//
// IMPORTANT NOTES:
// - Simulated exists only in Development, for trying the flow without Stripe
// - Values are stored; never renumber
// =========================================================
public enum PlatformCardProvider
{
    Stripe = 1,
    Simulated = 2
}
