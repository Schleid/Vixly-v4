namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformApprovalKind.cs
//
// PURPOSE:
// What an approval request is about (D-S24, D-S26).
//
// RESPONSIBILITIES:
// - Name each kind
//
// IMPORTANT NOTES:
// - Discounts, offers and credit notes go to the sales manager; plan versions and promotions to the CFO
// =========================================================
public enum PlatformApprovalKind
{
    Discount = 1,
    Offer = 2,
    Promotion = 3,
    PlanVersion = 4,
    CreditNote = 5
}
