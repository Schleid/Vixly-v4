using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.ListOperationsCashDrawers;

// =========================================================
// FILE: ListOperationsCashDrawersResponse.cs
//
// PURPOSE:
// Cash drawers (Slice 5F, C-4): the person's own, and every drawer of the warehouses where they confirm payments.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsCashDrawersResponse
{
    private ListOperationsCashDrawersResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsCashDrawer> Drawers { get; private init; } = Array.Empty<OperationsCashDrawer>();

    public IReadOnlyList<OperationsWarehouse> Warehouses { get; private init; } = Array.Empty<OperationsWarehouse>();

    public IReadOnlyDictionary<Guid, string> People { get; private init; } = new Dictionary<Guid, string>();

    public Guid UserId { get; private init; }

    public static ListOperationsCashDrawersResponse Success(IReadOnlyList<OperationsCashDrawer> drawers, IReadOnlyList<OperationsWarehouse> warehouses, IReadOnlyDictionary<Guid, string> people, Guid userId)
    {
        return new ListOperationsCashDrawersResponse { Succeeded = true, Drawers = drawers, Warehouses = warehouses, People = people, UserId = userId };
    }

    public static ListOperationsCashDrawersResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsCashDrawersResponse { Error = error };
    }
}
