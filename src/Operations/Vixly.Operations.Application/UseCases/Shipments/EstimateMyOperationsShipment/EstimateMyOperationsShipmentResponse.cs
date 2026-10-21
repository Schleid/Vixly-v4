using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Shipments.EstimateMyOperationsShipment;

// =========================================================
// FILE: EstimateMyOperationsShipmentResponse.cs
//
// PURPOSE:
// The shipment estimate the customer sees on the portal while building or changing a shipment, and on their shipment (Slice 5D).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class EstimateMyOperationsShipmentResponse
{
    private EstimateMyOperationsShipmentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Vixly.Operations.Application.Charges.OperationsEstimate? Estimate { get; private init; }

    public Vixly.Operations.Domain.Settings.OperationsWeightUnit WeightUnit { get; private init; }

    public Vixly.Operations.Domain.Settings.OperationsDimensionUnit DimensionUnit { get; private init; }

    public static EstimateMyOperationsShipmentResponse Success(Vixly.Operations.Application.Charges.OperationsEstimate? estimate, Vixly.Operations.Domain.Settings.OperationsWeightUnit weightUnit, Vixly.Operations.Domain.Settings.OperationsDimensionUnit dimensionUnit)
    {
        return new EstimateMyOperationsShipmentResponse { Succeeded = true, Estimate = estimate, WeightUnit = weightUnit, DimensionUnit = dimensionUnit };
    }

    public static EstimateMyOperationsShipmentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new EstimateMyOperationsShipmentResponse { Error = error };
    }
}
