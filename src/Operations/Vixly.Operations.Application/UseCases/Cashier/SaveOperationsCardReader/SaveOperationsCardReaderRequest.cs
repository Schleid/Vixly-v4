namespace Vixly.Operations.Application.UseCases.Cashier.SaveOperationsCardReader;

// =========================================================
// FILE: SaveOperationsCardReaderRequest.cs
//
// PURPOSE:
// Adds or removes a Stripe Terminal card reader of a warehouse (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsCardReaderRequest
{
    public Guid? RemoveReaderId { get; init; }

    public Guid WarehouseId { get; init; }

    public string? Label { get; init; }

    public string? StripeReaderId { get; init; }
}
