namespace Vixly.Operations.Application.UseCases.Site.RecordOperationsSiteTestimonial;

// =========================================================
// FILE: RecordOperationsSiteTestimonialRequest.cs
//
// PURPOSE:
// Records a customer's testimonial for the website, with the customer's agreement (owner, 2026-10-30).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RecordOperationsSiteTestimonialRequest
{
    public string? Name { get; init; }

    public string? Place { get; init; }

    public string? Quote { get; init; }

    public int? Rating { get; init; }

    public string? Source { get; init; }

    public bool CustomerAgreed { get; init; }
}
