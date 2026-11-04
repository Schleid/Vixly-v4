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

    // Customer reviews (owner, 2026-11-03): how many are new, whether the portal asks for them, and whether the plan has the portal.
    public int NewCount { get; private init; }

    public bool AskForReviews { get; private init; }

    public bool PortalInPlan { get; private init; }

    public static ListOperationsSiteTestimonialsResponse Success(IReadOnlyList<OperationsSiteTestimonial> testimonials, int newCount = 0, bool askForReviews = false, bool portalInPlan = false)
    {
        return new ListOperationsSiteTestimonialsResponse
        {
            Succeeded = true, Testimonials = testimonials, NewCount = newCount, AskForReviews = askForReviews, PortalInPlan = portalInPlan
        };
    }

    public static ListOperationsSiteTestimonialsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsSiteTestimonialsResponse { Error = error };
    }
}
