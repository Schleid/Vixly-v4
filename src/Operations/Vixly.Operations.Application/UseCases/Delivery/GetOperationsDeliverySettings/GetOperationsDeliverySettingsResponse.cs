using Vixly.Operations.Domain.Delivery;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Delivery.GetOperationsDeliverySettings;

// =========================================================
// FILE: GetOperationsDeliverySettingsResponse.cs
//
// PURPOSE:
// Settings → Delivery: how drivers record, cash on delivery, the tries, the proof at the door (Slice 7 step 3, R-12 to R-15).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsDeliverySettingsResponse
{
    private GetOperationsDeliverySettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsDeliveryRecording Recording { get; private init; }

    public bool CashOnDelivery { get; private init; }

    public int Tries { get; private init; }

    public OperationsAfterFailedDelivery AfterFailed { get; private init; }

    public OperationsDeliveryProofs Proofs { get; private init; }

    public bool Emails { get; private init; }

    public bool CanEdit { get; private init; }

    public static GetOperationsDeliverySettingsResponse Success(OperationsDeliveryRecording recording, bool cashOnDelivery, int tries, OperationsAfterFailedDelivery afterFailed, OperationsDeliveryProofs proofs, bool emails, bool canEdit)
    {
        return new GetOperationsDeliverySettingsResponse { Succeeded = true, Recording = recording, CashOnDelivery = cashOnDelivery, Tries = tries, AfterFailed = afterFailed, Proofs = proofs, Emails = emails, CanEdit = canEdit };
    }

    public static GetOperationsDeliverySettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsDeliverySettingsResponse { Error = error };
    }
}
