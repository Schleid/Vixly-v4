using Vixly.Operations.Application.Charges;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Charges.GetMyOperationsRates;

// =========================================================
// FILE: GetMyOperationsRatesResponse.cs
//
// PURPOSE:
// The customer's rate calculator to a zone and the estimate of their shipment, always an estimate (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetMyOperationsRatesResponse
{
    private GetMyOperationsRatesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsRating> Ratings { get; private init; } = Array.Empty<OperationsRating>();

    public IReadOnlyList<OperationsServiceOption> Services { get; private init; } = Array.Empty<OperationsServiceOption>();

    // Slice 4C: where the customer may ship from and to.
    public IReadOnlyList<OperationsWarehouse> Origins { get; private init; } = Array.Empty<OperationsWarehouse>();

    public IReadOnlyList<OperationsWarehouse> Destinations { get; private init; } = Array.Empty<OperationsWarehouse>();

    public OperationsWeightUnit WeightUnit { get; private init; }

    public OperationsDimensionUnit DimensionUnit { get; private init; }

    public string CurrencyCode { get; private init; } = string.Empty;

    public IReadOnlyList<OperationsQuote> Quotes { get; private init; } = Array.Empty<OperationsQuote>();

    public int Unmeasured { get; private init; }

    public OperationsWarehouse? ShipmentDestination { get; private init; }

    public static GetMyOperationsRatesResponse Success(IReadOnlyList<OperationsRating> ratings, IReadOnlyList<OperationsServiceOption> services, IReadOnlyList<OperationsWarehouse> origins, IReadOnlyList<OperationsWarehouse> destinations, OperationsWeightUnit weightUnit, OperationsDimensionUnit dimensionUnit, string currencyCode, IReadOnlyList<OperationsQuote> quotes, int unmeasured, OperationsWarehouse? shipmentDestination)
    {
        return new GetMyOperationsRatesResponse { Succeeded = true, Ratings = ratings, Services = services, Origins = origins, Destinations = destinations, WeightUnit = weightUnit, DimensionUnit = dimensionUnit, CurrencyCode = currencyCode, Quotes = quotes, Unmeasured = unmeasured, ShipmentDestination = shipmentDestination };
    }

    public static GetMyOperationsRatesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetMyOperationsRatesResponse { Error = error };
    }
}
