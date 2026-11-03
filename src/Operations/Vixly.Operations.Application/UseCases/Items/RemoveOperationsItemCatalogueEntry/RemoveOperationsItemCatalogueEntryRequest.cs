using Vixly.Operations.Application.Items;
using Vixly.Operations.Domain.Items;
namespace Vixly.Operations.Application.UseCases.Items.RemoveOperationsItemCatalogueEntry;

// =========================================================
// FILE: RemoveOperationsItemCatalogueEntryRequest.cs
//
// PURPOSE:
// Removes an item from the company's catalogue (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RemoveOperationsItemCatalogueEntryRequest
{
    public Guid EntryId { get; init; }
}
