using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.AddOperationsCounterSupply;

// =========================================================
// FILE: AddOperationsCounterSupplyResponse.cs
//
// PURPOSE:
// A supply added at the counter (Slice 5E, S-11): by scan or by name, to the shipment being processed or to an order of supplies.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class AddOperationsCounterSupplyResponse
{
    private AddOperationsCounterSupplyResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid SaleId { get; private init; }

    public bool Added { get; private init; }

    public bool LowStock { get; private init; }

    public string? Code { get; private init; }

    public IReadOnlyList<OperationsSupply> Choices { get; private init; } = Array.Empty<OperationsSupply>();

    public static AddOperationsCounterSupplyResponse Success(Guid saleId, bool added, bool lowStock, string? code, IReadOnlyList<OperationsSupply> choices)
    {
        return new AddOperationsCounterSupplyResponse { Succeeded = true, SaleId = saleId, Added = added, LowStock = lowStock, Code = code, Choices = choices };
    }

    public static AddOperationsCounterSupplyResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new AddOperationsCounterSupplyResponse { Error = error };
    }
}
