using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.CreateMyOperationsPreAlert;

// =========================================================
// FILE: CreateMyOperationsPreAlertResponse.cs
//
// PURPOSE:
// A customer announces one more package in one of their shipments (Slice 3B; owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CreateMyOperationsPreAlertResponse
{
    private CreateMyOperationsPreAlertResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid PreAlertId { get; private init; }

    public Guid? ParcelId { get; private init; }

    public static CreateMyOperationsPreAlertResponse Success(Guid preAlertId, Guid? parcelId)
    {
        return new CreateMyOperationsPreAlertResponse { Succeeded = true, PreAlertId = preAlertId, ParcelId = parcelId };
    }

    public static CreateMyOperationsPreAlertResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CreateMyOperationsPreAlertResponse { Error = error };
    }
}
