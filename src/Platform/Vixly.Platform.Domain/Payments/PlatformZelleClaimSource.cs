namespace Vixly.Platform.Domain.Payments;

// =========================================================
// FILE: PlatformZelleClaimSource.cs
//
// PURPOSE:
// Which checkout the customer reported the Zelle payment from.
//
// RESPONSIBILITIES:
// - Pay link from an email, or the self-registration payment step
//
// IMPORTANT NOTES:
// - Values are stored; never renumber
// =========================================================
public enum PlatformZelleClaimSource
{
    PaymentLink = 1,
    Registration = 2
}
