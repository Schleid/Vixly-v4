using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Shipments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Shipments.GetOperationsShipment;

// =========================================================
// FILE: GetOperationsShipmentResponse.cs
//
// PURPOSE:
// One shipment for staff: where it goes, its customer, its pre-alerts and each parcel with where it stands (Slice 5A).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsShipmentResponse
{
    private GetOperationsShipmentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsShipment? Shipment { get; private init; }

    public OperationsWarehouse? Origin { get; private init; }

    public string? DestinationName { get; private init; }

    public string? CustomerCode { get; private init; }

    public string? CustomerName { get; private init; }

    public IReadOnlyList<Vixly.Operations.Domain.Receiving.OperationsPreAlert> PreAlerts { get; private init; } = Array.Empty<Vixly.Operations.Domain.Receiving.OperationsPreAlert>();

    public IReadOnlyList<OperationsShipmentParcel> Parcels { get; private init; } = Array.Empty<OperationsShipmentParcel>();

    public IReadOnlyList<string> Services { get; private init; } = Array.Empty<string>();

    public bool CanChangeService { get; private init; }

    public Vixly.Operations.Domain.Settings.OperationsTenantSettings? Settings { get; private init; }

    public static GetOperationsShipmentResponse Success(OperationsShipment? shipment, OperationsWarehouse? origin, string? destinationName, string? customerCode, string? customerName, IReadOnlyList<Vixly.Operations.Domain.Receiving.OperationsPreAlert> preAlerts, IReadOnlyList<OperationsShipmentParcel> parcels, IReadOnlyList<string> services, bool canChangeService, Vixly.Operations.Domain.Settings.OperationsTenantSettings? settings)
    {
        return new GetOperationsShipmentResponse { Succeeded = true, Shipment = shipment, Origin = origin, DestinationName = destinationName, CustomerCode = customerCode, CustomerName = customerName, PreAlerts = preAlerts, Parcels = parcels, Services = services, CanChangeService = canChangeService, Settings = settings };
    }

    public static GetOperationsShipmentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsShipmentResponse { Error = error };
    }
}
