using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.GetOperationsRepackBox;

// =========================================================
// FILE: GetOperationsRepackBoxResponse.cs
//
// PURPOSE:
// One repack box (Slice 6A, D-4): its shipment's parcels, in the box or still to add, with their shelves.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsRepackBoxResponse
{
    private GetOperationsRepackBoxResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsRepackBox Box { get; private init; } = null!;

    public OperationsShipment Shipment { get; private init; } = null!;

    public OperationsWarehouse Warehouse { get; private init; } = null!;

    public IReadOnlyList<OperationsParcel> Parcels { get; private init; } = Array.Empty<OperationsParcel>();

    public IReadOnlyDictionary<Guid, string> Shelves { get; private init; } = new Dictionary<Guid, string>();

    public string? BundleNumber { get; private init; }

    public bool CanManage { get; private init; }

    public bool CanFill { get; private init; }

    public Vixly.Operations.Domain.Settings.OperationsWeightUnit WeightUnit { get; private init; }

    public Vixly.Operations.Domain.Settings.OperationsDimensionUnit DimensionUnit { get; private init; }

    public static GetOperationsRepackBoxResponse Success(OperationsRepackBox box, OperationsShipment shipment, OperationsWarehouse warehouse, IReadOnlyList<OperationsParcel> parcels, IReadOnlyDictionary<Guid, string> shelves, string? bundleNumber, bool canManage, bool canFill, Vixly.Operations.Domain.Settings.OperationsWeightUnit weightUnit, Vixly.Operations.Domain.Settings.OperationsDimensionUnit dimensionUnit)
    {
        return new GetOperationsRepackBoxResponse { Succeeded = true, Box = box, Shipment = shipment, Warehouse = warehouse, Parcels = parcels, Shelves = shelves, BundleNumber = bundleNumber, CanManage = canManage, CanFill = canFill, WeightUnit = weightUnit, DimensionUnit = dimensionUnit };
    }

    public static GetOperationsRepackBoxResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsRepackBoxResponse { Error = error };
    }
}
