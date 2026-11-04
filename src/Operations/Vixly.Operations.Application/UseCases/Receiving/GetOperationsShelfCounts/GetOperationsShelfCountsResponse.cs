using Vixly.Operations.Application.Receiving;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.GetOperationsShelfCounts;

// =========================================================
// FILE: GetOperationsShelfCountsResponse.cs
//
// PURPOSE:
// The shelf counts of the warehouses a person may count, or one of them (Slice 12D, owner H-7).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsShelfCountsResponse
{
    private GetOperationsShelfCountsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsShelfCountView> Counts { get; private init; } = Array.Empty<OperationsShelfCountView>();

    public IReadOnlyList<Vixly.Operations.Domain.Locations.OperationsWarehouse> Warehouses { get; private init; } = Array.Empty<Vixly.Operations.Domain.Locations.OperationsWarehouse>();

    public static GetOperationsShelfCountsResponse Success(IReadOnlyList<OperationsShelfCountView> counts, IReadOnlyList<Vixly.Operations.Domain.Locations.OperationsWarehouse> warehouses)
    {
        return new GetOperationsShelfCountsResponse { Succeeded = true, Counts = counts, Warehouses = warehouses };
    }

    public static GetOperationsShelfCountsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsShelfCountsResponse { Error = error };
    }
}
