using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.GetOperationsDispatch;

// =========================================================
// FILE: GetOperationsDispatchResponse.cs
//
// PURPOSE:
// One dispatch (Slice 6B, D-2 to D-4): its bundles, the bundles that fit it, its legs and the carriers.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsDispatchResponse
{
    private GetOperationsDispatchResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsDispatch Dispatch { get; private init; } = null!;

    public OperationsWarehouse Warehouse { get; private init; } = null!;

    public OperationsWarehouse? Destination { get; private init; }

    public IReadOnlyList<OperationsBundle> Bundles { get; private init; } = Array.Empty<OperationsBundle>();

    public IReadOnlyList<OperationsBundle> Candidates { get; private init; } = Array.Empty<OperationsBundle>();

    public IReadOnlyList<OperationsCarrier> Carriers { get; private init; } = Array.Empty<OperationsCarrier>();

    public Vixly.Operations.Domain.Settings.OperationsWeightUnit WeightUnit { get; private init; }

    public string? TimeZone { get; private init; }

    // Step 2: its shipments with their house numbers and cost shares.
    public IReadOnlyList<Vixly.Operations.Application.Dispatch.OperationsDispatchShipmentRow> Shipments { get; private init; } = Array.Empty<Vixly.Operations.Application.Dispatch.OperationsDispatchShipmentRow>();

    public static GetOperationsDispatchResponse Success(OperationsDispatch dispatch, OperationsWarehouse warehouse, OperationsWarehouse? destination, IReadOnlyList<OperationsBundle> bundles, IReadOnlyList<OperationsBundle> candidates, IReadOnlyList<OperationsCarrier> carriers, Vixly.Operations.Domain.Settings.OperationsWeightUnit weightUnit, string? timeZone, IReadOnlyList<Vixly.Operations.Application.Dispatch.OperationsDispatchShipmentRow> shipments)
    {
        return new GetOperationsDispatchResponse { Succeeded = true, Dispatch = dispatch, Warehouse = warehouse, Destination = destination, Bundles = bundles, Candidates = candidates, Carriers = carriers, WeightUnit = weightUnit, TimeZone = timeZone, Shipments = shipments };
    }

    public static GetOperationsDispatchResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsDispatchResponse { Error = error };
    }
}
