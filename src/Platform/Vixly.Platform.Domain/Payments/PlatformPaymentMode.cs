namespace Vixly.Platform.Domain.Payments;

// =========================================================
// FILE: PlatformPaymentMode.cs
//
// PURPOSE:
// Whether card payments are test payments or real money (D-P9).
//
// RESPONSIBILITIES:
// - Name the whether card payments are test payments or real money (D-P9)
//
// IMPORTANT NOTES:
// - Values are stored; never renumber
// =========================================================
public enum PlatformPaymentMode
{
    Test = 1,
    Live = 2
}
