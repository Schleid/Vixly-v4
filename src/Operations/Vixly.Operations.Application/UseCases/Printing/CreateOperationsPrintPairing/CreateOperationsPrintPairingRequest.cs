namespace Vixly.Operations.Application.UseCases.Printing.CreateOperationsPrintPairing;

// =========================================================
// FILE: CreateOperationsPrintPairingRequest.cs
//
// PURPOSE:
// Makes the one-time code a phone running Vixly Print scans to join a warehouse (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CreateOperationsPrintPairingRequest
{
    public Guid WarehouseId { get; init; }

    public string? Name { get; init; }
}
