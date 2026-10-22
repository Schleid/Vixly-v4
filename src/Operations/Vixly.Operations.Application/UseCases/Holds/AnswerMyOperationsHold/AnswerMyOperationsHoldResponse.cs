using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Holds.AnswerMyOperationsHold;

// =========================================================
// FILE: AnswerMyOperationsHoldResponse.cs
//
// PURPOSE:
// The customer answers a hold on their package: a document, the contents and value, or a choice for a damaged package (Slice 3E, Q-R7).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class AnswerMyOperationsHoldResponse
{
    private AnswerMyOperationsHoldResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid AnswerId { get; private init; }

    public static AnswerMyOperationsHoldResponse Success(Guid answerId)
    {
        return new AnswerMyOperationsHoldResponse { Succeeded = true, AnswerId = answerId };
    }

    public static AnswerMyOperationsHoldResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new AnswerMyOperationsHoldResponse { Error = error };
    }
}
