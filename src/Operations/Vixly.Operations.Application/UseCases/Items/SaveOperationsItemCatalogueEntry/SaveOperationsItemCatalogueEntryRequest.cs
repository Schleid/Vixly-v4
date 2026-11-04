using Vixly.Operations.Application.Items;
using Vixly.Operations.Domain.Items;
namespace Vixly.Operations.Application.UseCases.Items.SaveOperationsItemCatalogueEntry;

// =========================================================
// FILE: SaveOperationsItemCatalogueEntryRequest.cs
//
// PURPOSE:
// Adds or changes an item in the company's catalogue (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsItemCatalogueEntryRequest
{
    public Guid? EntryId { get; init; }

    public string? Description { get; init; }

    public string? Matches { get; init; }

    public string? HsCode { get; init; }

    public OperationsItemUnit Unit { get; init; }
}
