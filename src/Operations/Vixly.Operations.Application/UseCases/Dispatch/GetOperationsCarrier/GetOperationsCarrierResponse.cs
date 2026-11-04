using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.GetOperationsCarrier;

// =========================================================
// FILE: GetOperationsCarrierResponse.cs
//
// PURPOSE:
// One carrier with its rates (Slice 6B, D-1).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsCarrierResponse
{
    private GetOperationsCarrierResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsCarrier Carrier { get; private init; } = null!;

    public IReadOnlyList<OperationsCarrierRate> Rates { get; private init; } = Array.Empty<OperationsCarrierRate>();

    public IReadOnlyList<Vixly.Operations.Domain.Locations.OperationsWarehouse> Destinations { get; private init; } = Array.Empty<Vixly.Operations.Domain.Locations.OperationsWarehouse>();

    public IReadOnlyList<string> BundleTypes { get; private init; } = Array.Empty<string>();

    public string CurrencyCode { get; private init; } = string.Empty;

    public Vixly.Operations.Domain.Settings.OperationsWeightUnit WeightUnit { get; private init; }

    public DateOnly Today { get; private init; }

    public static GetOperationsCarrierResponse Success(OperationsCarrier carrier, IReadOnlyList<OperationsCarrierRate> rates, IReadOnlyList<Vixly.Operations.Domain.Locations.OperationsWarehouse> destinations, IReadOnlyList<string> bundleTypes, string currencyCode, Vixly.Operations.Domain.Settings.OperationsWeightUnit weightUnit, DateOnly today)
    {
        return new GetOperationsCarrierResponse { Succeeded = true, Carrier = carrier, Rates = rates, Destinations = destinations, BundleTypes = bundleTypes, CurrencyCode = currencyCode, WeightUnit = weightUnit, Today = today };
    }

    public static GetOperationsCarrierResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsCarrierResponse { Error = error };
    }
}
