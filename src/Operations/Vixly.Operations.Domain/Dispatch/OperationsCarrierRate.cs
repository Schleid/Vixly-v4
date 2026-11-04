using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Dispatch;

// =========================================================
// FILE: OperationsCarrierRate.cs
//
// PURPOSE:
// A carrier's rate to one destination from a day on (Slice 6B, D-1; owner, A-2 and A-7).
//
// RESPONSIBILITIES:
// - Air: a rate per chargeable kg with a minimum
// - Ocean: a rate per bundle, for one bundle type or any
//
// IMPORTANT NOTES:
// - A later start date replaces an earlier rate from that day; never edited, removed only before it starts
// - Amounts in the company's currency
// =========================================================
public sealed class OperationsCarrierRate : OperationsEntity
{
    public const decimal MaxAmount = 1_000_000m;

    private OperationsCarrierRate()
    {
        CountryCode = string.Empty;
    }

    private OperationsCarrierRate(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        CountryCode = string.Empty;
    }

    public Guid CarrierId { get; private set; }

    // A destination warehouse, or a country for home delivery.
    public Guid? DestinationWarehouseId { get; private set; }

    public string CountryCode { get; private set; }

    // Ocean: the bundle type the rate is for; none: any type.
    public string? BundleType { get; private set; }

    public decimal? PerKg { get; private set; }

    public decimal? Minimum { get; private set; }

    public decimal? PerBundle { get; private set; }

    public DateOnly StartsOn { get; private set; }

    public static OperationsCarrierRate Create(Guid tenantId, OperationsCarrier carrier, Guid? destinationWarehouseId, string? countryCode, string? bundleType, decimal? perKg, decimal? minimum,
        decimal? perBundle, DateOnly startsOn, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(carrier);
        string country;

        try
        {
            country = OperationsCountryCodes.Normalize(countryCode);
        }
        catch (DomainException)
        {
            throw new DomainException(OperationsDispatchErrors.DestinationRequired);
        }

        bool air = carrier.Kind == OperationsCarrierKind.Air;
        bool valid = air
            ? perKg is > 0m && perBundle is null && minimum is null or >= 0m
            : perBundle is > 0m && perKg is null && minimum is null;

        if (!valid || new[] { perKg, minimum, perBundle }.Any(amount => amount is > MaxAmount || (amount is { } value && decimal.Round(value, 4) != value)) || destinationWarehouseId == Guid.Empty)
        {
            throw new DomainException(OperationsDispatchErrors.RateInvalid);
        }

        return new OperationsCarrierRate(tenantId, utcNow)
        {
            CarrierId = carrier.Id,
            DestinationWarehouseId = destinationWarehouseId,
            CountryCode = country,
            BundleType = air ? null : Vixly.Operations.Domain.Bundles.OperationsBundle.TypeText(bundleType),
            PerKg = perKg,
            Minimum = minimum,
            PerBundle = perBundle,
            StartsOn = startsOn
        };
    }
}
