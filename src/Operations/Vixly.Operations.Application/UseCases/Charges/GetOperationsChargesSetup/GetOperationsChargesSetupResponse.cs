using Vixly.Operations.Application.Charges;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Charges.GetOperationsChargesSetup;

// =========================================================
// FILE: GetOperationsChargesSetupResponse.cs
//
// PURPOSE:
// The company's price lists, extras, insurance plan and item types (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsChargesSetupResponse
{
    private GetOperationsChargesSetupResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsRateCardRow> Cards { get; private init; } = Array.Empty<OperationsRateCardRow>();

    public IReadOnlyList<OperationsChargeExtra> Extras { get; private init; } = Array.Empty<OperationsChargeExtra>();

    // Slice 4C: one plan per mode.
    public IReadOnlyList<OperationsInsurancePlan> Insurance { get; private init; } = Array.Empty<OperationsInsurancePlan>();

    public IReadOnlyList<string> Services { get; private init; } = Array.Empty<string>();

    public IReadOnlyList<OperationsItemType> ItemTypes { get; private init; } = Array.Empty<OperationsItemType>();

    // Slice 4C: the company's origins (drop-off) and destinations, for routes.
    public IReadOnlyList<OperationsWarehouse> Origins { get; private init; } = Array.Empty<OperationsWarehouse>();

    public IReadOnlyList<OperationsWarehouse> Destinations { get; private init; } = Array.Empty<OperationsWarehouse>();

    public OperationsWeightUnit WeightUnit { get; private init; }

    public OperationsDimensionUnit DimensionUnit { get; private init; }

    public string CurrencyCode { get; private init; } = string.Empty;

    public string? TimeZone { get; private init; }

    public DateTimeOffset UtcNow { get; private init; }

    public static GetOperationsChargesSetupResponse Success(IReadOnlyList<OperationsRateCardRow> cards, IReadOnlyList<OperationsChargeExtra> extras, IReadOnlyList<OperationsInsurancePlan> insurance, IReadOnlyList<string> services, IReadOnlyList<OperationsItemType> itemTypes, IReadOnlyList<OperationsWarehouse> origins, IReadOnlyList<OperationsWarehouse> destinations, OperationsWeightUnit weightUnit, OperationsDimensionUnit dimensionUnit, string currencyCode, string? timeZone, DateTimeOffset utcNow)
    {
        return new GetOperationsChargesSetupResponse { Succeeded = true, Cards = cards, Extras = extras, Insurance = insurance, Services = services, ItemTypes = itemTypes, Origins = origins, Destinations = destinations, WeightUnit = weightUnit, DimensionUnit = dimensionUnit, CurrencyCode = currencyCode, TimeZone = timeZone, UtcNow = utcNow };
    }

    public static GetOperationsChargesSetupResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsChargesSetupResponse { Error = error };
    }
}
