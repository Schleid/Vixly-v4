using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Inspection;

// =========================================================
// FILE: OperationsRestrictedItem.cs
//
// PURPOSE:
// One entry of a company's restricted-items list (Slice 3C design section 3).
//
// RESPONSIBILITIES:
// - Keep its name, the words that point to it in a declaration, prohibited or restricted, and what is needed
// - Say where it applies: all destinations or chosen countries, all carriers or chosen ones
// - Say whether a declaration names it
//
// IMPORTANT NOTES:
// - Each company owns its list; the starting list is copied, never shared (D-R5)
// - Never deleted: an item is taken out of use, so past inspections keep their meaning
// =========================================================
public sealed class OperationsRestrictedItem : OperationsEntity
{
    private OperationsRestrictedItem()
    {
        Name = string.Empty;
        Words = string.Empty;
    }

    private OperationsRestrictedItem(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        Name = string.Empty;
        Words = string.Empty;
    }

    public string Name { get; private set; }

    // Comma-separated words or phrases, as typed.
    public string Words { get; private set; }

    public OperationsRestrictedItemKind Kind { get; private set; }

    // Comma-separated ISO country codes; empty for every destination.
    public string? Countries { get; private set; }

    // Comma-separated carrier names; empty for every carrier.
    public string? Carriers { get; private set; }

    public string? Requirement { get; private set; }

    public bool IsActive { get; private set; }

    public static OperationsRestrictedItem Create(Guid tenantId, string? name, string? words, OperationsRestrictedItemKind kind, string? countries, string? carriers, string? requirement,
        DateTimeOffset utcNow)
    {
        OperationsRestrictedItem item = new(tenantId, utcNow) { IsActive = true };
        item.Set(name, words, kind, countries, carriers, requirement);
        return item;
    }

    public void Change(string? name, string? words, OperationsRestrictedItemKind kind, string? countries, string? carriers, string? requirement, bool active, DateTimeOffset utcNow)
    {
        Set(name, words, kind, countries, carriers, requirement);
        IsActive = active;
        MarkAsModified(utcNow);
    }

    // Applies to this destination and carrier; an empty list means all.
    public bool AppliesTo(string? countryCode, string? carrier) =>
        IsActive
        && (Countries is null || (countryCode is not null && CountryList(Countries).Contains(countryCode, StringComparer.Ordinal)))
        && (Carriers is null || (carrier is not null && OperationsDeclarationText.Phrases(Carriers).Any(phrase => phrase.SequenceEqual(OperationsDeclarationText.Words(carrier)))));

    public bool IsNamedIn(IReadOnlyList<string> declaration) =>
        OperationsDeclarationText.Phrases(Words).Any(phrase => OperationsDeclarationText.Contains(declaration, phrase));

    public static IReadOnlyList<string> CountryList(string? countries) =>
        (countries ?? string.Empty).Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private void Set(string? name, string? words, OperationsRestrictedItemKind kind, string? countries, string? carriers, string? requirement)
    {
        Name = OperationsDeclarationText.Required(name, OperationsInspectionLimits.NameMaxLength, OperationsInspectionErrors.NameRequired);
        string list = OperationsDeclarationText.Required(words, OperationsInspectionLimits.WordsMaxLength, OperationsInspectionErrors.WordsRequired);

        if (OperationsDeclarationText.Phrases(list) is not { Count: > 0 and <= OperationsInspectionLimits.MaxWords })
        {
            throw new DomainException(OperationsInspectionErrors.WordsRequired);
        }

        Words = string.Join(", ", list.Split([',', ';', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        Kind = Enum.IsDefined(kind) ? kind : throw new DomainException(OperationsInspectionErrors.RuleKindInvalid);
        Countries = CheckCountries(countries);
        string? carrierList = OperationsDeclarationText.Optional(carriers, OperationsInspectionLimits.CarriersMaxLength);
        Carriers = carrierList is null ? null : string.Join(", ", carrierList.Split([',', ';', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        Requirement = OperationsDeclarationText.Optional(requirement, OperationsInspectionLimits.RequirementMaxLength);
    }

    private static string? CheckCountries(string? countries)
    {
        IReadOnlyList<string> codes = CountryList(countries?.ToUpperInvariant());

        if (codes.Count == 0)
        {
            return null;
        }

        try
        {
            string joined = string.Join(",", codes.Select(OperationsCountryCodes.Normalize).Distinct(StringComparer.Ordinal));
            return joined.Length <= OperationsInspectionLimits.CountriesMaxLength ? joined : throw new DomainException(OperationsInspectionErrors.CountryInvalid);
        }
        catch (DomainException)
        {
            throw new DomainException(OperationsInspectionErrors.CountryInvalid);
        }
    }
}
