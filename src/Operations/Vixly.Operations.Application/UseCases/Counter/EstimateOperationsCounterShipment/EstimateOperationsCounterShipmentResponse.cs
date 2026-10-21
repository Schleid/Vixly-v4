using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.EstimateOperationsCounterShipment;

// =========================================================
// FILE: EstimateOperationsCounterShipmentResponse.cs
//
// PURPOSE:
// The shipment estimate while the clerk fills in a new shipment, before any parcel (Slice 5D).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class EstimateOperationsCounterShipmentResponse
{
    private EstimateOperationsCounterShipmentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Vixly.Operations.Application.Charges.OperationsEstimate? Estimate { get; private init; }

    public Vixly.Operations.Domain.Settings.OperationsWeightUnit WeightUnit { get; private init; }

    public Vixly.Operations.Domain.Settings.OperationsDimensionUnit DimensionUnit { get; private init; }

    public static EstimateOperationsCounterShipmentResponse Success(Vixly.Operations.Application.Charges.OperationsEstimate? estimate, Vixly.Operations.Domain.Settings.OperationsWeightUnit weightUnit, Vixly.Operations.Domain.Settings.OperationsDimensionUnit dimensionUnit)
    {
        return new EstimateOperationsCounterShipmentResponse { Succeeded = true, Estimate = estimate, WeightUnit = weightUnit, DimensionUnit = dimensionUnit };
    }

    public static EstimateOperationsCounterShipmentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new EstimateOperationsCounterShipmentResponse { Error = error };
    }
}
