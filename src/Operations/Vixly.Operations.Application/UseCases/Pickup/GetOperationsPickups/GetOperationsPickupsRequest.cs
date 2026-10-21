namespace Vixly.Operations.Application.UseCases.Pickup.GetOperationsPickups;

// =========================================================
// FILE: GetOperationsPickupsRequest.cs
//
// PURPOSE:
// The shipments ready for pickup at a destination warehouse (Slice 7 step 2, R-9).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsPickupsRequest
{
    public Guid? WarehouseId { get; init; }

    public string? Query { get; init; }
}
