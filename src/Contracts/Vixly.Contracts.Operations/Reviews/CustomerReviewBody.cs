namespace Vixly.Contracts.Operations.Reviews;

// =========================================================
// FILE: CustomerReviewBody.cs
//
// PURPOSE:
// The customer writes or changes their review (owner, 2026-11-03).
//
// RESPONSIBILITIES:
// - Carry the review as the customer wrote it
//
// IMPORTANT NOTES:
// - NameStyle: FirstAndInitial or FirstOnly; Agreed: the company may show it on its website
// - Wire type: add fields at the end, never rename (ADR-0014)
// =========================================================
public sealed record CustomerReviewBody(int? Rating, string? Quote, string? Place, string? NameStyle, bool Agreed);
