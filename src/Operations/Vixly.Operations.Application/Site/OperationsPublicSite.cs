using Vixly.Operations.Domain.Branding;

namespace Vixly.Operations.Application.Site;

// =========================================================
// FILE: OperationsPublicSite.cs
//
// PURPOSE:
// Everything a tenant site page shows (Slice 1B).
//
// RESPONSIBILITIES:
// - Carry the settings, texts, locations and contacts of one language
//
// IMPORTANT NOTES:
// - Built from the published version, or the draft for an Owner preview
// =========================================================
public sealed record OperationsPublicSite(
    OperationsBrandSettings Settings,
    IReadOnlyDictionary<string, string> Texts,
    int Version,
    bool IsPreview,
    IReadOnlyList<OperationsPublicLocation> Locations,
    IReadOnlyList<OperationsPublicContact> Contacts)
{
    public string? Text(string key) => Texts.TryGetValue(key, out string? value) ? value : null;

    // The prices the company publishes (owner, 2026-10-21); none while it does not.
    // Only the Pricing page carries the lists; every page knows whether the menu shows Pricing.
    public Vixly.Operations.Application.Charges.OperationsPricingView? Pricing { get; init; }

    public bool PricingShown { get; init; }

    // Countries in display order: those with locations or contacts, each once.
    public IReadOnlyList<string> Countries =>
        Locations.Select(location => location.CountryCode).Concat(Contacts.Select(contact => contact.CountryCode)).Distinct(StringComparer.Ordinal).ToList();
}
