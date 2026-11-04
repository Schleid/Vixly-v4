using Vixly.Operations.Domain.Supplies;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.GetOperationsSupply;

// =========================================================
// FILE: GetOperationsSupplyResponse.cs
//
// PURPOSE:
// One shipping supply with its barcodes and prices (Slice 5E, S-3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsSupplyResponse
{
    private GetOperationsSupplyResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsSupply Supply { get; private init; } = null!;

    public IReadOnlyList<OperationsWarehouse> Warehouses { get; private init; } = Array.Empty<OperationsWarehouse>();

    public string CurrencyCode { get; private init; } = string.Empty;

    public DateTimeOffset UtcNow { get; private init; }

    public static GetOperationsSupplyResponse Success(OperationsSupply supply, IReadOnlyList<OperationsWarehouse> warehouses, string currencyCode, DateTimeOffset utcNow)
    {
        return new GetOperationsSupplyResponse { Succeeded = true, Supply = supply, Warehouses = warehouses, CurrencyCode = currencyCode, UtcNow = utcNow };
    }

    public static GetOperationsSupplyResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsSupplyResponse { Error = error };
    }
}
