using System.Globalization;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Inspection;

// =========================================================
// FILE: OperationsInspectionRule.cs
//
// PURPOSE:
// One of a company's inspection rules: matching parcels go to compliance inspection (Slice 3C design section 3).
//
// RESPONSIBILITIES:
// - Keep the kind, its value and whether it is in use
// - Say whether a parcel matches, and why, in words the station shows
//
// IMPORTANT NOTES:
// - ListedItems has no value: any item of the list named in the declaration matches
// - ValueAtLeast compares the declared value in the company's currency
// - Never deleted: taken out of use
// =========================================================
public sealed class OperationsInspectionRule : OperationsEntity
{
    private OperationsInspectionRule()
    {
    }

    private OperationsInspectionRule(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
    }

    public OperationsInspectionRuleKind Kind { get; private set; }

    // An amount ("500"), a carrier name ("DHL") or a country code ("HT"); none for ListedItems.
    public string? Value { get; private set; }

    public bool IsActive { get; private set; }

    public static OperationsInspectionRule Create(Guid tenantId, OperationsInspectionRuleKind kind, string? value, DateTimeOffset utcNow) =>
        new(tenantId, utcNow) { Kind = CheckKind(kind), Value = CheckValue(kind, value), IsActive = true };

    public void ChangeActive(bool active, DateTimeOffset utcNow)
    {
        IsActive = active;
        MarkAsModified(utcNow);
    }

    public decimal? Amount => Kind == OperationsInspectionRuleKind.ValueAtLeast && decimal.TryParse(Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal amount)
        ? amount
        : null;

    // Value, carrier and destination rules; a ListedItems rule is matched by OperationsInspectionSelector with the company's list.
    public bool Matches(OperationsInspectionSubject subject)
    {
        ArgumentNullException.ThrowIfNull(subject);

        return IsActive && Kind switch
        {
            OperationsInspectionRuleKind.ValueAtLeast => subject.DeclaredValue is { } declared && Amount is { } amount && declared >= amount,
            OperationsInspectionRuleKind.Carrier => subject.Carrier is not null && OperationsDeclarationText.Words(subject.Carrier).SequenceEqual(OperationsDeclarationText.Words(Value)),
            OperationsInspectionRuleKind.DestinationCountry => subject.CountryCode is not null && subject.CountryCode == Value,
            _ => false
        };
    }

    private static OperationsInspectionRuleKind CheckKind(OperationsInspectionRuleKind kind) =>
        Enum.IsDefined(kind) ? kind : throw new DomainException(OperationsInspectionErrors.RuleKindInvalid);

    private static string? CheckValue(OperationsInspectionRuleKind kind, string? value)
    {
        string text = (value ?? string.Empty).Trim();

        return CheckKind(kind) switch
        {
            OperationsInspectionRuleKind.ListedItems => null,
            OperationsInspectionRuleKind.ValueAtLeast => text.Length <= 12 && text.All(character => char.IsAsciiDigit(character) || character == '.')
                && decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal amount) && amount > 0 && amount <= 1_000_000m
                ? decimal.Round(amount, 2, MidpointRounding.AwayFromZero).ToString("0.##", CultureInfo.InvariantCulture)
                : throw new DomainException(OperationsInspectionErrors.RuleValueInvalid),
            OperationsInspectionRuleKind.DestinationCountry => Country(text),
            _ => OperationsDeclarationText.Optional(text, OperationsInspectionLimits.RuleValueMaxLength) ?? throw new DomainException(OperationsInspectionErrors.RuleValueInvalid)
        };
    }

    private static string Country(string text)
    {
        try
        {
            return OperationsCountryCodes.Normalize(text);
        }
        catch (DomainException)
        {
            throw new DomainException(OperationsInspectionErrors.CountryInvalid);
        }
    }
}
