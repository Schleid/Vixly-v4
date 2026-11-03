using Vixly.Operations.Domain.Branding;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Site.ListOperationsSiteTestimonials;

// =========================================================
// FILE: ListOperationsSiteTestimonialsResponse.cs
//
// PURPOSE:
// The company's customer testimonials for the website editor (owner, 2026-10-30).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsSiteTestimonialsResponse
{
    private ListOperationsSiteTestimonialsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsSiteTestimonial> Testimonials { get; private init; } = Array.Empty<OperationsSiteTestimonial>();

    public static ListOperationsSiteTestimonialsResponse Success(IReadOnlyList<OperationsSiteTestimonial> testimonials)
    {
        return new ListOperationsSiteTestimonialsResponse { Succeeded = true, Testimonials = testimonials };
    }

    public static ListOperationsSiteTestimonialsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsSiteTestimonialsResponse { Error = error };
    }
}
