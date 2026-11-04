namespace Vixly.Operations.Application.UseCases.Dispatch.CreateOperationsDispatch;

// =========================================================
// FILE: CreateOperationsDispatchRequest.cs
//
// PURPOSE:
// Plan a dispatch from a warehouse to a destination by a service (Slice 6B, D-2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CreateOperationsDispatchRequest
{
    public Guid WarehouseId { get; init; }

    public Guid? DestinationWarehouseId { get; init; }

    public string? CountryCode { get; init; }

    public string? ServiceCode { get; init; }

    // Where the company works.
    public DateTime? Cutoff { get; init; }

    public string? Notes { get; init; }

    public Guid? RequestKey { get; init; }
}
