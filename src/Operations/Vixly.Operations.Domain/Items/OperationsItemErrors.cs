using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Items;

// =========================================================
// FILE: OperationsItemErrors.cs
//
// PURPOSE:
// Error codes of the items in a parcel and the item catalogue (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Name every refusal of an item, a classification or a catalogue entry
//
// IMPORTANT NOTES:
// - Codes, never text (L-1)
// =========================================================
public static class OperationsItemErrors
{
    public static Error WordsRequired { get; } = Error.Create("ops.items.words_required", ("maxLength", OperationsParcelItem.WordsMaxLength));

    public static Error QuantityInvalid { get; } = Error.Create("ops.items.quantity_invalid");

    public static Error ValueInvalid { get; } = Error.Create("ops.items.value_invalid");

    public static Error TooMany { get; } = Error.Create("ops.items.too_many", ("max", OperationsParcelItem.MaxPerParcel));

    public static Error CodeInvalid { get; } = Error.Create("ops.items.code_invalid");

    public static Error OriginInvalid { get; } = Error.Create("ops.items.origin_invalid");

    public static Error UnitInvalid { get; } = Error.Create("ops.items.unit_invalid");

    public static Error DescriptionInvalid { get; } = Error.Create("ops.items.description_invalid", ("maxLength", OperationsParcelItem.DescriptionMaxLength));

    public static Error MatchesInvalid { get; } = Error.Create("ops.items.matches_invalid", ("maxLength", OperationsItemCatalogueEntry.MatchesMaxLength));

    public static Error CatalogueFull { get; } = Error.Create("ops.items.catalogue_full", ("max", OperationsItemCatalogueEntry.MaxPerCompany));

    public static Error CatalogueEntryNotFound { get; } = Error.Create("ops.items.catalogue_not_found");

    public static Error NotFound { get; } = Error.Create("ops.items.not_found");

    // A parcel cannot go into a bundle or leave on a dispatch while an item has no customs code or country of origin.
    public static Error Incomplete(string parcelNumber, string item) => Error.Create("ops.items.incomplete", ("parcel", parcelNumber), ("item", item));
}
