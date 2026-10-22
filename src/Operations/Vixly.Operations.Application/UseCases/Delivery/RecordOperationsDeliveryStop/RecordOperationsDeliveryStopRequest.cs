using Vixly.Operations.Domain.Pickup;

namespace Vixly.Operations.Application.UseCases.Delivery.RecordOperationsDeliveryStop;

// =========================================================
// FILE: RecordOperationsDeliveryStopRequest.cs
//
// PURPOSE:
// Record how a stop went: delivered with the proof the company asks and the cash collected, or failed with a reason (Slice 7 step 3, R-7, R-12 to R-15).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RecordOperationsDeliveryStopRequest
{
    public Guid RouteId { get; init; }

    public Guid StopId { get; init; }

    public bool Delivered { get; init; }

    public string? ReceiverName { get; init; }

    public OperationsPickupIdKind? IdKind { get; init; }

    public string? IdLast { get; init; }

    public byte[]? Signature { get; init; }

    public byte[]? Photo { get; init; }

    public string? Code { get; init; }

    public decimal? Cash { get; init; }

    public string? Failure { get; init; }

    public string? Note { get; init; }

    public string? PortalSlug { get; init; }
}
