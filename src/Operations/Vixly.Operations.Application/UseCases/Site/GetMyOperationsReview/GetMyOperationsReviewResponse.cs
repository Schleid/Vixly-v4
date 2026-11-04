using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Site.GetMyOperationsReview;

// =========================================================
// FILE: GetMyOperationsReviewResponse.cs
//
// PURPOSE:
// Result of GetMyOperationsReview.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetMyOperationsReviewResponse
{
    private GetMyOperationsReviewResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public bool Asked { get; private init; }

    public bool Eligible { get; private init; }

    public Vixly.Operations.Domain.Branding.OperationsSiteTestimonial? Review { get; private init; }

    public bool InvitePending { get; private init; }

    public static GetMyOperationsReviewResponse Success(bool asked, bool eligible, Vixly.Operations.Domain.Branding.OperationsSiteTestimonial? review, bool invitePending)
    {
        return new GetMyOperationsReviewResponse { Succeeded = true, Asked = asked, Eligible = eligible, Review = review, InvitePending = invitePending };
    }

    public static GetMyOperationsReviewResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetMyOperationsReviewResponse { Error = error };
    }
}
