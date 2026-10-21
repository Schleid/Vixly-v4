using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.ListMyOperationsPackages;

// =========================================================
// FILE: ListMyOperationsPackagesResponse.cs
//
// PURPOSE:
// A customer's pre-alerts and packages, newest first (Slice 3B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListMyOperationsPackagesResponse
{
    private ListMyOperationsPackagesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsPreAlertView> PreAlerts { get; private init; } = Array.Empty<OperationsPreAlertView>();

    public IReadOnlyList<OperationsCustomerParcel> Parcels { get; private init; } = Array.Empty<OperationsCustomerParcel>();

    public OperationsWeightUnit WeightUnit { get; private init; }

    public OperationsDimensionUnit DimensionUnit { get; private init; }

    public IReadOnlyList<Vixly.Operations.Application.Shipments.OperationsCustomerShipment> Shipments { get; private init; } = Array.Empty<Vixly.Operations.Application.Shipments.OperationsCustomerShipment>();

    public static ListMyOperationsPackagesResponse Success(IReadOnlyList<OperationsPreAlertView> preAlerts, IReadOnlyList<OperationsCustomerParcel> parcels, OperationsWeightUnit weightUnit, OperationsDimensionUnit dimensionUnit, IReadOnlyList<Vixly.Operations.Application.Shipments.OperationsCustomerShipment> shipments)
    {
        return new ListMyOperationsPackagesResponse { Succeeded = true, PreAlerts = preAlerts, Parcels = parcels, WeightUnit = weightUnit, DimensionUnit = dimensionUnit, Shipments = shipments };
    }

    public static ListMyOperationsPackagesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListMyOperationsPackagesResponse { Error = error };
    }
}
