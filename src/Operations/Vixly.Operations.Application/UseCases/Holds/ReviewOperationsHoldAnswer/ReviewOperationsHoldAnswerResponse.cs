using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Holds.ReviewOperationsHoldAnswer;

// =========================================================
// FILE: ReviewOperationsHoldAnswerResponse.cs
//
// PURPOSE:
// Staff accept a customer's answer to a hold, which ends it, or decline it with what is still missing (Slice 3E).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ReviewOperationsHoldAnswerResponse
{
    private ReviewOperationsHoldAnswerResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ReviewOperationsHoldAnswerResponse Success()
    {
        return new ReviewOperationsHoldAnswerResponse { Succeeded = true };
    }

    public static ReviewOperationsHoldAnswerResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ReviewOperationsHoldAnswerResponse { Error = error };
    }
}
