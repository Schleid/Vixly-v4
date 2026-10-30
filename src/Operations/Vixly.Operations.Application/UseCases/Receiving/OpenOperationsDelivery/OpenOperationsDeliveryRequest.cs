namespace Vixly.Operations.Application.UseCases.Receiving.OpenOperationsDelivery;

// =========================================================
// FILE: OpenOperationsDeliveryRequest.cs
//
// PURPOSE:
// Opens a carrier delivery at the dock: the packages one driver brings (Slice 3B, owner 2026-10-18).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class OpenOperationsDeliveryRequest
{
    public Guid WarehouseId { get; init; }

    public string? Carrier { get; init; }

    public string CommandKey { get; init; } = string.Empty;
}
