using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.CheckOperationsParcel;

// =========================================================
// FILE: CheckOperationsParcelResponse.cs
//
// PURPOSE:
// Records a parcel at the inspection station: condition, weight, size and photos; picks some for compliance inspection (Slice 3B, owner 2026-10-18).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CheckOperationsParcelResponse
{
    private CheckOperationsParcelResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid ParcelId { get; private init; }

    public bool SelectedForInspection { get; private init; }

    // Slice 4D step 2: the shipping label went to the station's printer.
    public bool ShippingLabelPrinted { get; private init; }

    public static CheckOperationsParcelResponse Success(Guid parcelId, bool selectedForInspection, bool shippingLabelPrinted = false)
    {
        return new CheckOperationsParcelResponse { Succeeded = true, ParcelId = parcelId, SelectedForInspection = selectedForInspection, ShippingLabelPrinted = shippingLabelPrinted };
    }

    public static CheckOperationsParcelResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CheckOperationsParcelResponse { Error = error };
    }
}
