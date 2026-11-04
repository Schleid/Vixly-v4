using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.CancelMyOperationsPreAlert;

// =========================================================
// FILE: CancelMyOperationsPreAlertResponse.cs
//
// PURPOSE:
// A customer withdraws a pre-alert that has not been received (Slice 3B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CancelMyOperationsPreAlertResponse
{
    private CancelMyOperationsPreAlertResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid PreAlertId { get; private init; }

    public static CancelMyOperationsPreAlertResponse Success(Guid preAlertId)
    {
        return new CancelMyOperationsPreAlertResponse { Succeeded = true, PreAlertId = preAlertId };
    }

    public static CancelMyOperationsPreAlertResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CancelMyOperationsPreAlertResponse { Error = error };
    }
}
