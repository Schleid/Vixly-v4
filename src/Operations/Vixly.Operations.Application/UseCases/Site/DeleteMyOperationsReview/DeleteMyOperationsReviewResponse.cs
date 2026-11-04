using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Site.DeleteMyOperationsReview;

// =========================================================
// FILE: DeleteMyOperationsReviewResponse.cs
//
// PURPOSE:
// Result of DeleteMyOperationsReview.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class DeleteMyOperationsReviewResponse
{
    private DeleteMyOperationsReviewResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static DeleteMyOperationsReviewResponse Success()
    {
        return new DeleteMyOperationsReviewResponse { Succeeded = true };
    }

    public static DeleteMyOperationsReviewResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new DeleteMyOperationsReviewResponse { Error = error };
    }
}
