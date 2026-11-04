using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Site.DismissMyOperationsReviewInvite;

// =========================================================
// FILE: DismissMyOperationsReviewInviteResponse.cs
//
// PURPOSE:
// Result of DismissMyOperationsReviewInvite.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class DismissMyOperationsReviewInviteResponse
{
    private DismissMyOperationsReviewInviteResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static DismissMyOperationsReviewInviteResponse Success()
    {
        return new DismissMyOperationsReviewInviteResponse { Succeeded = true };
    }

    public static DismissMyOperationsReviewInviteResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new DismissMyOperationsReviewInviteResponse { Error = error };
    }
}
