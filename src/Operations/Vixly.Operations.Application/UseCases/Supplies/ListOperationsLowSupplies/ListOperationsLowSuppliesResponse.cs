using Vixly.Operations.Application.Supplies;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.ListOperationsLowSupplies;

// =========================================================
// FILE: ListOperationsLowSuppliesResponse.cs
//
// PURPOSE:
// The supplies at or below their reorder level in the warehouses a person keeps the stock of (Slice 5E, S-8), for the staff home page.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsLowSuppliesResponse
{
    private ListOperationsLowSuppliesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsLowSupply> Items { get; private init; } = Array.Empty<OperationsLowSupply>();

    public static ListOperationsLowSuppliesResponse Success(IReadOnlyList<OperationsLowSupply> items)
    {
        return new ListOperationsLowSuppliesResponse { Succeeded = true, Items = items };
    }

    public static ListOperationsLowSuppliesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsLowSuppliesResponse { Error = error };
    }
}
