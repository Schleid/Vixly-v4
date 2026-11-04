using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.UseCases.Inspection.FinishOperationsInspection;

// =========================================================
// FILE: FinishOperationsInspectionRequest.cs
//
// PURPOSE:
// Records what the inspector found and decided: passed, held for the customer, repacked or refused (Slice 3C design section 5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class FinishOperationsInspectionRequest
{
    public Guid InspectionId { get; init; }

    public string Result { get; init; } = string.Empty;

    public IReadOnlyList<Guid> ItemIds { get; init; } = Array.Empty<Guid>();

    public string? Notes { get; init; }

    public string? CustomerAction { get; init; }

    public OperationsHoldReasons HoldReason { get; init; }

    public decimal? Fee { get; init; }

    public DateTimeOffset? CalledCustomerUtc { get; init; }

    public string? CallNotes { get; init; }

    public IReadOnlyList<OperationsParcelPhotoUpload> Photos { get; init; } = Array.Empty<OperationsParcelPhotoUpload>();

    public string CommandKey { get; init; } = string.Empty;

    public string PortalSlug { get; init; } = string.Empty;
}
