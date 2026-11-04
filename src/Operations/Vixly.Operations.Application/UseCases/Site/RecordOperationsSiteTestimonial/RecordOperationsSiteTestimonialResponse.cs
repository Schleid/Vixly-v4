using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Site.RecordOperationsSiteTestimonial;

// =========================================================
// FILE: RecordOperationsSiteTestimonialResponse.cs
//
// PURPOSE:
// Records a customer's testimonial for the website, with the customer's agreement (owner, 2026-10-30).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RecordOperationsSiteTestimonialResponse
{
    private RecordOperationsSiteTestimonialResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid TestimonialId { get; private init; }

    public static RecordOperationsSiteTestimonialResponse Success(Guid testimonialId)
    {
        return new RecordOperationsSiteTestimonialResponse { Succeeded = true, TestimonialId = testimonialId };
    }

    public static RecordOperationsSiteTestimonialResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RecordOperationsSiteTestimonialResponse { Error = error };
    }
}
