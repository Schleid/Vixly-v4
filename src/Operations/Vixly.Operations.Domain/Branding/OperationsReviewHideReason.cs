namespace Vixly.Operations.Domain.Branding;

// =========================================================
// FILE: OperationsReviewHideReason.cs
//
// PURPOSE:
// Why a company hid a customer review (owner, 2026-11-03).
//
// RESPONSIBILITIES:
// - Name the reasons a review may be held back whatever its rating
//
// IMPORTANT NOTES:
// - Stored as a number; the grounds of the FTC rule on reviews (16 CFR 465.7), plus other; never shown to the customer
// =========================================================
public enum OperationsReviewHideReason
{
    PersonalInformation = 1,
    Abusive = 2,
    NotAboutService = 3,
    SuspectedFake = 4,
    Other = 5
}
