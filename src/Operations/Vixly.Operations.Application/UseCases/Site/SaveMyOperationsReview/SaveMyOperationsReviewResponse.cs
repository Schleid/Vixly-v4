using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Site.SaveMyOperationsReview;

// =========================================================
// FILE: SaveMyOperationsReviewResponse.cs
//
// PURPOSE:
// Result of SaveMyOperationsReview.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveMyOperationsReviewResponse
{
    private SaveMyOperationsReviewResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SaveMyOperationsReviewResponse Success()
    {
        return new SaveMyOperationsReviewResponse { Succeeded = true };
    }

    public static SaveMyOperationsReviewResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveMyOperationsReviewResponse { Error = error };
    }
}
