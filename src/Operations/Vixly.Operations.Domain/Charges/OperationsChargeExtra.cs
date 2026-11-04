using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Inspection;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsChargeExtra.cs
//
// PURPOSE:
// An extra charge of the company, with dated amounts (owner, Slice 4: "with some flexibility"; Slice 4B: per zone and delivery, effective dates; Slice 4C: per mode and route).
//
// RESPONSIBILITIES:
// - Keep the name, whether it is a government fee passed through, when it applies and where: mode, service, country, route, delivery
// - Schedule an amount; find the amount in force at a moment
//
// IMPORTANT NOTES:
// - Never deleted, taken out of use
// - A government fee is shown apart and never discounted (O-10)
// =========================================================
public sealed class OperationsChargeExtra : OperationsEntity
{
    private readonly List<OperationsExtraVersion> _versions = new();

    private OperationsChargeExtra()
    {
        Name = string.Empty;
    }

    private OperationsChargeExtra(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        Name = string.Empty;
    }

    public string Name { get; private set; }

    public bool IsGovernmentFee { get; private set; }

    public OperationsExtraTrigger Trigger { get; private set; }

    // Comma-separated words or phrases, for an extra applied when the declaration names it.
    public string? Words { get; private set; }

    // Null: every service.
    public string? ServiceCode { get; private set; }

    // Null: every destination country.
    public string? CountryCode { get; private set; }

    // Slice 4C: null: every mode (air, ocean, ground).
    public OperationsTransportMode? Mode { get; private set; }

    // Slice 4C: null: any origin; null: any destination.
    public Guid? OriginWarehouseId { get; private set; }

    public Guid? DestinationWarehouseId { get; private set; }

    // None: pickup and home delivery alike.
    public OperationsDeliveryModes Delivery { get; private set; }

    public IReadOnlyList<OperationsExtraVersion> Versions => _versions;

    public bool IsActive { get; private set; }

    public static OperationsChargeExtra Create(Guid tenantId, OperationsExtraTerms terms, decimal amount, OperationsExtraCalculation calculation, OperationsExtraPer per,
        DateTimeOffset? effectiveFrom, DateTimeOffset utcNow)
    {
        OperationsChargeExtra extra = new(tenantId, utcNow) { IsActive = true };
        extra.Set(terms);
        extra.Schedule(amount, calculation, per, effectiveFrom, utcNow);
        return extra;
    }

    public void Change(OperationsExtraTerms terms, bool active, DateTimeOffset utcNow)
    {
        Set(terms);
        IsActive = active;
        MarkAsModified(utcNow);
    }

    public OperationsExtraVersion Schedule(decimal amount, OperationsExtraCalculation calculation, OperationsExtraPer per, DateTimeOffset? effectiveFrom, DateTimeOffset utcNow)
    {
        OperationsExtraVersion version = OperationsExtraVersion.Create(amount, calculation, per, OperationsVersions.Start(effectiveFrom, utcNow));
        OperationsVersions.Schedule(_versions, version, utcNow);
        MarkAsModified(utcNow);
        return version;
    }

    public void RemoveScheduled(Guid versionId, DateTimeOffset utcNow)
    {
        OperationsVersions.Remove(_versions, versionId, utcNow);
        MarkAsModified(utcNow);
    }

    public OperationsExtraVersion? VersionAt(DateTimeOffset at) => OperationsVersions.At(_versions, at);

    public bool AppliesTo(string serviceCode, string? countryCode, Guid? originWarehouseId, Guid? destinationWarehouseId, OperationsDeliveryModes delivery) =>
        IsActive && (Mode is null || Mode == OperationsTransportModes.Of(serviceCode)) && (ServiceCode is null || ServiceCode == serviceCode)
        && (CountryCode is null || CountryCode == countryCode) && (OriginWarehouseId is null || OriginWarehouseId == originWarehouseId)
        && (DestinationWarehouseId is null || DestinationWarehouseId == destinationWarehouseId)
        && (Delivery == OperationsDeliveryModes.None || (delivery != OperationsDeliveryModes.None && Delivery.HasFlag(delivery)));

    public bool IsNamedIn(string? contents) =>
        Words is not null && contents is not null
        && OperationsDeclarationText.Phrases(Words).Any(phrase => OperationsDeclarationText.Contains(OperationsDeclarationText.Words(contents), phrase));

    private void Set(OperationsExtraTerms terms)
    {
        ArgumentNullException.ThrowIfNull(terms);

        if (!Enum.IsDefined(terms.Trigger) || (terms.Mode is { } mode && !Enum.IsDefined(mode)) || (terms.Delivery & ~(OperationsDeliveryModes.Pickup | OperationsDeliveryModes.HomeDelivery)) != OperationsDeliveryModes.None)
        {
            throw new DomainException(OperationsChargeErrors.ChoiceInvalid);
        }

        string? words = OperationsChargeLimits.Text(terms.Words, OperationsChargeLimits.WordsMaxLength, OperationsChargeErrors.TextInvalid);

        if (terms.Trigger == OperationsExtraTrigger.Declared && (words is null || OperationsDeclarationText.Phrases(words).Count == 0))
        {
            throw new DomainException(OperationsChargeErrors.WordsRequired);
        }

        string? country;

        try
        {
            country = string.IsNullOrWhiteSpace(terms.CountryCode) ? null : OperationsCountryCodes.Normalize(terms.CountryCode);
        }
        catch (DomainException)
        {
            throw new DomainException(OperationsChargeErrors.CountryInvalid);
        }

        string? service = string.IsNullOrWhiteSpace(terms.ServiceCode) ? null : terms.ServiceCode.Trim();

        Name = OperationsChargeLimits.Text(terms.Name, OperationsChargeLimits.NameMaxLength, OperationsChargeErrors.TextInvalid) ?? throw new DomainException(OperationsChargeErrors.NameRequired);
        IsGovernmentFee = terms.IsGovernmentFee;
        Trigger = terms.Trigger;
        Words = terms.Trigger == OperationsExtraTrigger.Declared ? words : null;
        ServiceCode = service is { Length: > OperationsChargeLimits.ServiceCodeMaxLength } ? throw new DomainException(OperationsChargeErrors.ServiceUnknown) : service;
        CountryCode = country;
        Mode = terms.Mode;
        OriginWarehouseId = terms.OriginWarehouseId == Guid.Empty ? null : terms.OriginWarehouseId;
        DestinationWarehouseId = terms.DestinationWarehouseId == Guid.Empty ? null : terms.DestinationWarehouseId;
        Delivery = terms.Delivery;
    }
}
