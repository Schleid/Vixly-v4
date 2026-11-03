namespace Vixly.Operations.Application.UseCases.Receiving.StartOperationsShelfCount;

// =========================================================
// FILE: StartOperationsShelfCountRequest.cs
//
// PURPOSE:
// Starts the count of one shelf, by its scanned code (Slice 12D, owner H-7); an open count of that shelf goes on.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class StartOperationsShelfCountRequest
{
    public Guid? WarehouseId { get; init; }

    public string? ShelfCode { get; init; }
}
