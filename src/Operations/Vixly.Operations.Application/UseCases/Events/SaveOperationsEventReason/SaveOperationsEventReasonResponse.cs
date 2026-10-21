using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Events.SaveOperationsEventReason;

// =========================================================
// FILE: SaveOperationsEventReasonResponse.cs
//
// PURPOSE:
// Add a cause of the company's own, or change one (Slice 6C, E-3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsEventReasonResponse
{
    private SaveOperationsEventReasonResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid ReasonId { get; private init; }

    public static SaveOperationsEventReasonResponse Success(Guid reasonId)
    {
        return new SaveOperationsEventReasonResponse { Succeeded = true, ReasonId = reasonId };
    }

    public static SaveOperationsEventReasonResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsEventReasonResponse { Error = error };
    }
}
