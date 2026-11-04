using Vixly.Operations.Application.Items;
using Vixly.Operations.Domain.Items;
namespace Vixly.Operations.Application.UseCases.Items.SaveOperationsParcelItems;

// =========================================================
// FILE: SaveOperationsParcelItemsRequest.cs
//
// PURPOSE:
// Staff classify a parcel's items for customs: description on documents, customs code, country of origin, unit (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsParcelItemsRequest
{
    public Guid ParcelId { get; init; }

    public IReadOnlyList<OperationsItemClassification> Items { get; init; } = Array.Empty<OperationsItemClassification>();
}
