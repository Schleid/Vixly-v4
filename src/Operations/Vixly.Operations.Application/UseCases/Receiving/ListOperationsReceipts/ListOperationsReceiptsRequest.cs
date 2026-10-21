namespace Vixly.Operations.Application.UseCases.Receiving.ListOperationsReceipts;

// =========================================================
// FILE: ListOperationsReceiptsRequest.cs
//
// PURPOSE:
// The carrier deliveries of the warehouses a person receives at: open ones, then receipts (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListOperationsReceiptsRequest
{
    public int Page { get; init; } = 1;
}
