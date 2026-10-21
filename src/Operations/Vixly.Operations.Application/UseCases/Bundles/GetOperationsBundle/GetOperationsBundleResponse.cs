using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.GetOperationsBundle;

// =========================================================
// FILE: GetOperationsBundleResponse.cs
//
// PURPOSE:
// One bundle (Slice 6A, D-10): its parcels, its shipments' completeness, and the shipment of the last scan.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsBundleResponse
{
    private GetOperationsBundleResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsBundle Bundle { get; private init; } = null!;

    public OperationsWarehouse Warehouse { get; private init; } = null!;

    public OperationsWarehouse? Destination { get; private init; }

    public IReadOnlyList<OperationsBundleParcelRow> Parcels { get; private init; } = Array.Empty<OperationsBundleParcelRow>();

    public IReadOnlyList<OperationsBundleShipmentLine> Shipments { get; private init; } = Array.Empty<OperationsBundleShipmentLine>();

    public OperationsBundleShipmentLine? Focus { get; private init; }

    public bool CanManage { get; private init; }

    public bool CanFill { get; private init; }

    public Vixly.Operations.Domain.Settings.OperationsWeightUnit WeightUnit { get; private init; }

    public Vixly.Operations.Domain.Settings.OperationsDimensionUnit DimensionUnit { get; private init; }

    public decimal? Target { get; private init; }

    public static GetOperationsBundleResponse Success(OperationsBundle bundle, OperationsWarehouse warehouse, OperationsWarehouse? destination, IReadOnlyList<OperationsBundleParcelRow> parcels, IReadOnlyList<OperationsBundleShipmentLine> shipments, OperationsBundleShipmentLine? focus, bool canManage, bool canFill, Vixly.Operations.Domain.Settings.OperationsWeightUnit weightUnit, Vixly.Operations.Domain.Settings.OperationsDimensionUnit dimensionUnit, decimal? target)
    {
        return new GetOperationsBundleResponse { Succeeded = true, Bundle = bundle, Warehouse = warehouse, Destination = destination, Parcels = parcels, Shipments = shipments, Focus = focus, CanManage = canManage, CanFill = canFill, WeightUnit = weightUnit, DimensionUnit = dimensionUnit, Target = target };
    }

    public static GetOperationsBundleResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsBundleResponse { Error = error };
    }
}
