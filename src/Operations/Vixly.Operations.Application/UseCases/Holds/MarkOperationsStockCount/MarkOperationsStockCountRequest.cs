using Vixly.Operations.Domain.Holds;

namespace Vixly.Operations.Application.UseCases.Holds.MarkOperationsStockCount;

// =========================================================
// FILE: MarkOperationsStockCountRequest.cs
//
// PURPOSE:
// Marks a parcel of a count found on its shelf (picked or scanned) or missing, with a note (Slice 4D step 3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class MarkOperationsStockCountRequest
{
    public Guid CountId { get; init; }

    public Guid? ParcelId { get; init; }

    // The parcel ID label scanned at the shelf.
    public string? Code { get; init; }

    public OperationsCountResult Result { get; init; } = OperationsCountResult.Found;

    public string? Note { get; init; }
}
