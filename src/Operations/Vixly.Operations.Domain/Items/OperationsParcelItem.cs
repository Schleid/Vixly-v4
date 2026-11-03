using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Items;

// =========================================================
// FILE: OperationsParcelItem.cs
//
// PURPOSE:
// One item in a parcel: what the customer declared, and how staff classified it for customs (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - The customer's declaration: their own words, quantity, value of one, currency, new or used
// - Staff's classification: the description on documents, the customs code, the country of origin and the unit, with who and when
// - Belongs to a pre-alert until the package arrives, then to its parcel
//
// IMPORTANT NOTES:
// - D-C6: the declaration is the customer's; staff never change it, they only classify
// - Complete when it has a customs code and a country of origin
// - Plain text only; every page encodes output
// =========================================================
public sealed class OperationsParcelItem : OperationsEntity
{
    public const int WordsMaxLength = 200;

    public const int DescriptionMaxLength = 200;

    public const int MaxPerParcel = 30;

    public const decimal MaxQuantity = 100_000m;

    public const decimal MaxValue = 10_000_000m;

    private OperationsParcelItem()
    {
        Words = string.Empty;
        CurrencyCode = string.Empty;
    }

    private OperationsParcelItem(Guid tenantId, DateTimeOffset createdUtc)
        : base(tenantId, createdUtc)
    {
        Words = string.Empty;
        CurrencyCode = string.Empty;
    }

    public Guid? ParcelId { get; private set; }

    public Guid? PreAlertId { get; private set; }

    public int Line { get; private set; }

    // The customer's own words, kept as typed.
    public string Words { get; private set; }

    public decimal Quantity { get; private set; }

    public decimal ValueEach { get; private set; }

    public string CurrencyCode { get; private set; }

    public bool Used { get; private set; }

    public string? Description { get; private set; }

    public string? HsCode { get; private set; }

    public string? OriginCountryCode { get; private set; }

    public OperationsItemUnit Unit { get; private set; } = OperationsItemUnit.Pieces;

    public Guid? CatalogueEntryId { get; private set; }

    public Guid? ClassifiedByUserId { get; private set; }

    public DateTimeOffset? ClassifiedUtc { get; private set; }

    public bool IsComplete => HsCode is not null && OriginCountryCode is not null;

    public decimal Total => Quantity * ValueEach;

    public static OperationsParcelItem Declare(Guid tenantId, string? words, decimal quantity, decimal valueEach, string currencyCode, bool used, int line, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);

        if (quantity <= 0 || quantity > MaxQuantity || decimal.Round(quantity, 3) != quantity)
        {
            throw new DomainException(OperationsItemErrors.QuantityInvalid);
        }

        if (valueEach < 0 || valueEach > MaxValue)
        {
            throw new DomainException(OperationsItemErrors.ValueInvalid);
        }

        if (line < 1 || line > MaxPerParcel)
        {
            throw new DomainException(OperationsItemErrors.TooMany);
        }

        return new OperationsParcelItem(tenantId, utcNow)
        {
            Words = Text(words, WordsMaxLength) ?? throw new DomainException(OperationsItemErrors.WordsRequired),
            Quantity = quantity,
            ValueEach = decimal.Round(valueEach, 2),
            CurrencyCode = currencyCode,
            Used = used,
            Line = line
        };
    }

    public void ForPreAlert(Guid preAlertId, DateTimeOffset utcNow)
    {
        PreAlertId = preAlertId;
        MarkAsModified(utcNow);
    }

    // The package arrived (or was handed in at the counter): the item is the parcel's from now on.
    public void ForParcel(Guid parcelId, DateTimeOffset utcNow)
    {
        ParcelId = parcelId;
        MarkAsModified(utcNow);
    }

    // The catalogue knew the customer's words: description, code and unit from it; the country of origin is never guessed.
    public void Suggest(OperationsItemCatalogueEntry entry, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(entry);
        (Description, HsCode, Unit, CatalogueEntryId) = (entry.Description, entry.HsCode, entry.Unit, entry.Id);
        MarkAsModified(utcNow);
    }

    // Staff classified it: the declaration itself never changes.
    public void Classify(string? description, string? hsCode, string? originCountryCode, OperationsItemUnit unit, Guid? catalogueEntryId, Guid userId, DateTimeOffset utcNow)
    {
        if (!Enum.IsDefined(unit))
        {
            throw new DomainException(OperationsItemErrors.UnitInvalid);
        }

        string? shown = Text(description, DescriptionMaxLength);

        if (description is not null && shown is null && description.Trim().Length > 0)
        {
            throw new DomainException(OperationsItemErrors.DescriptionInvalid);
        }

        string? origin;

        try
        {
            origin = string.IsNullOrWhiteSpace(originCountryCode) ? null : Vixly.Operations.Domain.Locations.OperationsCountryCodes.Normalize(originCountryCode);
        }
        catch (DomainException)
        {
            throw new DomainException(OperationsItemErrors.OriginInvalid);
        }

        (Description, HsCode, OriginCountryCode, Unit, CatalogueEntryId, ClassifiedByUserId, ClassifiedUtc) =
            (shown, OperationsHsCode.Normalize(hsCode), origin, unit, catalogueEntryId, userId, utcNow);
        MarkAsModified(utcNow);
    }

    // Trimmed text without markup, at most the given length; null when empty; too long or with markup is refused.
    public static string? Text(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string text = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return text.Length > maxLength || text.Contains('<', StringComparison.Ordinal) || text.Contains('>', StringComparison.Ordinal) ? null : text;
    }
}
