using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Site.ChangeOperationsReviewInvitations;

// =========================================================
// FILE: ChangeOperationsReviewInvitationsResponse.cs
//
// PURPOSE:
// Result of ChangeOperationsReviewInvitations.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeOperationsReviewInvitationsResponse
{
    private ChangeOperationsReviewInvitationsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ChangeOperationsReviewInvitationsResponse Success()
    {
        return new ChangeOperationsReviewInvitationsResponse { Succeeded = true };
    }

    public static ChangeOperationsReviewInvitationsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeOperationsReviewInvitationsResponse { Error = error };
    }
}
