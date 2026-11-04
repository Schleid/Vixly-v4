namespace Vixly.Operations.Application.UseCases.Site.ChangeOperationsSiteTestimonial;

// =========================================================
// FILE: ChangeOperationsSiteTestimonialRequest.cs
//
// PURPOSE:
// Shows, hides or removes a customer testimonial on the website (owner, 2026-10-30).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ChangeOperationsSiteTestimonialRequest
{
    public Guid TestimonialId { get; init; }

    public string? Action { get; init; }

    // Customer reviews (owner, 2026-11-03): why the company hides one.
    public Vixly.Operations.Domain.Branding.OperationsReviewHideReason? HideReason { get; init; }
}
