using Vixly.Operations.Domain.Items;

namespace Vixly.Operations.Application.Items;

// =========================================================
// FILE: OperationsItemClassification.cs
//
// PURPOSE:
// How staff classify one item for customs (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Carry the item, the description on documents, the customs code, the country of origin, the unit, and whether to add it to the catalogue
//
// IMPORTANT NOTES:
// - No item: the parcel's single declaration, made an item when saved
// =========================================================
public sealed record OperationsItemClassification(Guid? ItemId, string? Description, string? HsCode, string? OriginCountryCode, OperationsItemUnit Unit, bool AddToCatalogue);
