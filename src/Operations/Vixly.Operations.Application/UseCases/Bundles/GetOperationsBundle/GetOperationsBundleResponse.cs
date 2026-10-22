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

    // The bundle's list (owner, 2026-10-21): its lines, and what may be added from the computer.
    public IReadOnlyList<OperationsBundleListRow> List { get; private init; } = Array.Empty<OperationsBundleListRow>();

    public IReadOnlyList<OperationsBundleCandidate> Candidates { get; private init; } = Array.Empty<OperationsBundleCandidate>();

    public static GetOperationsBundleResponse Success(OperationsBundle bundle, OperationsWarehouse warehouse, OperationsWarehouse? destination, IReadOnlyList<OperationsBundleParcelRow> parcels, IReadOnlyList<OperationsBundleShipmentLine> shipments, OperationsBundleShipmentLine? focus, bool canManage, bool canFill, Vixly.Operations.Domain.Settings.OperationsWeightUnit weightUnit, Vixly.Operations.Domain.Settings.OperationsDimensionUnit dimensionUnit, decimal? target, IReadOnlyList<OperationsBundleListRow>? list = null, IReadOnlyList<OperationsBundleCandidate>? candidates = null)
    {
        return new GetOperationsBundleResponse { Succeeded = true, Bundle = bundle, Warehouse = warehouse, Destination = destination, Parcels = parcels, Shipments = shipments, Focus = focus, CanManage = canManage, CanFill = canFill, WeightUnit = weightUnit, DimensionUnit = dimensionUnit, Target = target , List = list ?? Array.Empty<OperationsBundleListRow>(), Candidates = candidates ?? Array.Empty<OperationsBundleCandidate>() };
    }

    public static GetOperationsBundleResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsBundleResponse { Error = error };
    }
}
