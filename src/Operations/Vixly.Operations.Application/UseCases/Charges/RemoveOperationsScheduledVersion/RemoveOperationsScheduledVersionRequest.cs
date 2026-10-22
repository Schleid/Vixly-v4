using Vixly.Operations.Application.Charges;

namespace Vixly.Operations.Application.UseCases.Charges.RemoveOperationsScheduledVersion;

// =========================================================
// FILE: RemoveOperationsScheduledVersionRequest.cs
//
// PURPOSE:
// Removes a version that has not started yet: a price list, an extra or the insurance plan (Slice 4B design section 5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RemoveOperationsScheduledVersionRequest
{
    public OperationsVersionOwner Owner { get; init; } = OperationsVersionOwner.PriceList;

    public Guid OwnerId { get; init; }

    public Guid VersionId { get; init; }
}
