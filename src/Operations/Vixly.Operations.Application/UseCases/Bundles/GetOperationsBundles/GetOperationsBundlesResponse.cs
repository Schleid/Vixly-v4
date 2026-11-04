using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.GetOperationsBundles;

// =========================================================
// FILE: GetOperationsBundlesResponse.cs
//
// PURPOSE:
// The bundles desk (Slice 6A, D-10): parcels ready to bundle by destination and service, the open and closed bundles.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsBundlesResponse
{
    private GetOperationsBundlesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsWarehouse> Warehouses { get; private init; } = Array.Empty<OperationsWarehouse>();

    public IReadOnlyList<OperationsWarehouse> AllWarehouses { get; private init; } = Array.Empty<OperationsWarehouse>();

    public IReadOnlyList<OperationsReadyGroup> Ready { get; private init; } = Array.Empty<OperationsReadyGroup>();

    public IReadOnlyList<OperationsBundle> Bundles { get; private init; } = Array.Empty<OperationsBundle>();

    public IReadOnlyList<string> Services { get; private init; } = Array.Empty<string>();

    public IReadOnlyList<string> Types { get; private init; } = Array.Empty<string>();

    public IReadOnlyList<Guid> ManageWarehouses { get; private init; } = Array.Empty<Guid>();

    public Vixly.Operations.Domain.Settings.OperationsWeightUnit WeightUnit { get; private init; }

    public IReadOnlyDictionary<string, decimal?> Divisors { get; private init; } = new Dictionary<string, decimal?>();

    public static GetOperationsBundlesResponse Success(IReadOnlyList<OperationsWarehouse> warehouses, IReadOnlyList<OperationsWarehouse> allWarehouses, IReadOnlyList<OperationsReadyGroup> ready, IReadOnlyList<OperationsBundle> bundles, IReadOnlyList<string> services, IReadOnlyList<string> types, IReadOnlyList<Guid> manageWarehouses, Vixly.Operations.Domain.Settings.OperationsWeightUnit weightUnit, IReadOnlyDictionary<string, decimal?> divisors)
    {
        return new GetOperationsBundlesResponse { Succeeded = true, Warehouses = warehouses, AllWarehouses = allWarehouses, Ready = ready, Bundles = bundles, Services = services, Types = types, ManageWarehouses = manageWarehouses, WeightUnit = weightUnit, Divisors = divisors };
    }

    public static GetOperationsBundlesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsBundlesResponse { Error = error };
    }
}
