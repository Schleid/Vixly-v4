namespace Vixly.Contracts.Operations.Reviews;

// =========================================================
// FILE: CustomerReviewDto.cs
//
// PURPOSE:
// The customer's review in the portal, and whether they may write one (owner, 2026-11-03).
//
// RESPONSIBILITIES:
// - Carry the values between Customer.Web and Operations.Api
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// - Status: New, Published or Hidden; why a review was hidden is never sent
// =========================================================
public sealed record CustomerReviewDto(bool Asked, bool Eligible, bool InvitePending, bool HasReview, int? Rating, string? Quote, string? Place, string NameStyle, bool Agreed,
    string? Status, string? ShownName, DateTimeOffset? SubmittedUtc);
