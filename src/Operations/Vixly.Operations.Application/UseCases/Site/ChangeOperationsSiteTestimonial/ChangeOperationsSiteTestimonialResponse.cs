using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Site.ChangeOperationsSiteTestimonial;

// =========================================================
// FILE: ChangeOperationsSiteTestimonialResponse.cs
//
// PURPOSE:
// Shows, hides or removes a customer testimonial on the website (owner, 2026-10-30).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeOperationsSiteTestimonialResponse
{
    private ChangeOperationsSiteTestimonialResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ChangeOperationsSiteTestimonialResponse Success()
    {
        return new ChangeOperationsSiteTestimonialResponse { Succeeded = true };
    }

    public static ChangeOperationsSiteTestimonialResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeOperationsSiteTestimonialResponse { Error = error };
    }
}
