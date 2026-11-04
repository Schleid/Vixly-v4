using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.UseCases.Inspection.ReleaseOperationsHold;

// =========================================================
// FILE: ReleaseOperationsHoldRequest.cs
//
// PURPOSE:
// Releases a hold with a note, for example "customer sent the invoice by email" (Slice 3C design section 6).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ReleaseOperationsHoldRequest
{
    public Guid ParcelId { get; init; }

    public OperationsHoldReasons Reason { get; init; }

    public string? Note { get; init; }

    public string CommandKey { get; init; } = string.Empty;
}
