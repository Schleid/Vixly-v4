using Vixly.Operations.Domain.Branding;

namespace Vixly.Operations.Application.Site;

// =========================================================
// FILE: OperationsReviewState.cs
//
// PURPOSE:
// Where a customer stands with reviews (owner, 2026-11-03).
//
// RESPONSIBILITIES:
// - Say whether the company asks for reviews, whether this customer may write one, their review, and whether the portal invites them
//
// IMPORTANT NOTES:
// - Read-only
// =========================================================
public sealed record OperationsReviewState(bool Asked, bool Eligible, OperationsSiteTestimonial? Review, bool InvitePending)
{
    public bool MayWrite => Asked && Eligible;
}
