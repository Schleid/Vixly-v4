using Vixly.Operations.Application.Charges;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Charges.PriceOperationsShipment;

// =========================================================
// FILE: PriceOperationsShipmentResponse.cs
//
// PURPOSE:
// Prices a shipment for a customer to a zone, on one service or every offered one, for a quote (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class PriceOperationsShipmentResponse
{
    private PriceOperationsShipmentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsRating> Ratings { get; private init; } = Array.Empty<OperationsRating>();

    public Guid? CustomerId { get; private init; }

    public string? CustomerName { get; private init; }

    public IReadOnlyList<OperationsChargeExtra> ManualExtras { get; private init; } = Array.Empty<OperationsChargeExtra>();

    public IReadOnlyList<OperationsServiceOption> Services { get; private init; } = Array.Empty<OperationsServiceOption>();

    // Slice 4C: the company's origins and destinations, for the route.
    public IReadOnlyList<OperationsWarehouse> Origins { get; private init; } = Array.Empty<OperationsWarehouse>();

    public IReadOnlyList<OperationsWarehouse> Destinations { get; private init; } = Array.Empty<OperationsWarehouse>();

    public IReadOnlyList<OperationsItemType> ItemTypes { get; private init; } = Array.Empty<OperationsItemType>();

    public OperationsWeightUnit WeightUnit { get; private init; }

    public OperationsDimensionUnit DimensionUnit { get; private init; }

    public string CurrencyCode { get; private init; } = string.Empty;

    public static PriceOperationsShipmentResponse Success(IReadOnlyList<OperationsRating> ratings, Guid? customerId, string? customerName, IReadOnlyList<OperationsChargeExtra> manualExtras, IReadOnlyList<OperationsServiceOption> services, IReadOnlyList<OperationsWarehouse> origins, IReadOnlyList<OperationsWarehouse> destinations, IReadOnlyList<OperationsItemType> itemTypes, OperationsWeightUnit weightUnit, OperationsDimensionUnit dimensionUnit, string currencyCode)
    {
        return new PriceOperationsShipmentResponse { Succeeded = true, Ratings = ratings, CustomerId = customerId, CustomerName = customerName, ManualExtras = manualExtras, Services = services, Origins = origins, Destinations = destinations, ItemTypes = itemTypes, WeightUnit = weightUnit, DimensionUnit = dimensionUnit, CurrencyCode = currencyCode };
    }

    public static PriceOperationsShipmentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new PriceOperationsShipmentResponse { Error = error };
    }
}
