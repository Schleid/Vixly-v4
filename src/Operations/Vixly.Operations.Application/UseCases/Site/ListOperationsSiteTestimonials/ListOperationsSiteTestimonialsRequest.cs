namespace Vixly.Operations.Application.UseCases.Site.ListOperationsSiteTestimonials;

// =========================================================
// FILE: ListOperationsSiteTestimonialsRequest.cs
//
// PURPOSE:
// The company's customer testimonials for the website editor (owner, 2026-10-30).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListOperationsSiteTestimonialsRequest
{
    // Customer reviews (owner, 2026-11-03): new, published, hidden or all.
    public Vixly.Operations.Application.Site.OperationsTestimonialView View { get; init; }
}
