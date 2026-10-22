using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Supplies;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.ListOperationsSupplies;

// =========================================================
// FILE: ListOperationsSuppliesResponse.cs
//
// PURPOSE:
// The company's shipping supplies with their current price (Slice 5E, S-3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsSuppliesResponse
{
    private ListOperationsSuppliesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsSupply> Supplies { get; private init; } = Array.Empty<OperationsSupply>();

    public IReadOnlyList<OperationsWarehouse> Warehouses { get; private init; } = Array.Empty<OperationsWarehouse>();

    public string CurrencyCode { get; private init; } = string.Empty;

    public DateTimeOffset UtcNow { get; private init; }

    public bool CountsStock { get; private init; } = true;

    public OperationsSupplyStockOut StockOut { get; private init; } = OperationsSupplyStockOut.AllowWithWarning;

    // Slice 5E (S-17): the company settings, for the settings form.
    public OperationsTenantSettings? Settings { get; private init; }

    public static ListOperationsSuppliesResponse Success(IReadOnlyList<OperationsSupply> supplies, IReadOnlyList<OperationsWarehouse> warehouses, string currencyCode, DateTimeOffset utcNow, bool countsStock, OperationsSupplyStockOut stockOut, OperationsTenantSettings? settings = null)
    {
        return new ListOperationsSuppliesResponse { Succeeded = true, Supplies = supplies, Warehouses = warehouses, CurrencyCode = currencyCode, UtcNow = utcNow, CountsStock = countsStock, StockOut = stockOut, Settings = settings };
    }

    public static ListOperationsSuppliesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsSuppliesResponse { Error = error };
    }
}
