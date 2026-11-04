using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Shipments.GetMyOperationsShipment;

// =========================================================
// FILE: GetMyOperationsShipmentResponse.cs
//
// PURPOSE:
// A customer's shipment with its announced and arrived packages (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetMyOperationsShipmentResponse
{
    private GetMyOperationsShipmentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsCustomerShipment? Shipment { get; private init; }

    public IReadOnlyList<OperationsPreAlertView> PreAlerts { get; private init; } = Array.Empty<OperationsPreAlertView>();

    public IReadOnlyList<OperationsCustomerParcel> Parcels { get; private init; } = Array.Empty<OperationsCustomerParcel>();

    public bool CanCancel { get; private init; }

    public OperationsWeightUnit WeightUnit { get; private init; }

    public OperationsDimensionUnit DimensionUnit { get; private init; }

    public static GetMyOperationsShipmentResponse Success(OperationsCustomerShipment? shipment, IReadOnlyList<OperationsPreAlertView> preAlerts, IReadOnlyList<OperationsCustomerParcel> parcels, bool canCancel, OperationsWeightUnit weightUnit, OperationsDimensionUnit dimensionUnit)
    {
        return new GetMyOperationsShipmentResponse { Succeeded = true, Shipment = shipment, PreAlerts = preAlerts, Parcels = parcels, CanCancel = canCancel, WeightUnit = weightUnit, DimensionUnit = dimensionUnit };
    }

    public static GetMyOperationsShipmentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetMyOperationsShipmentResponse { Error = error };
    }
}
