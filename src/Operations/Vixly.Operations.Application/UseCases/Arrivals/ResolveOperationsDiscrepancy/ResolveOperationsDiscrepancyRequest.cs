using Vixly.Operations.Domain.Arrivals;

namespace Vixly.Operations.Application.UseCases.Arrivals.ResolveOperationsDiscrepancy;

// =========================================================
// FILE: ResolveOperationsDiscrepancyRequest.cs
//
// PURPOSE:
// Resolve a discrepancy: lost (the customer is told) or settled, with a note (Slice 7, R-2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ResolveOperationsDiscrepancyRequest
{
    public Guid DiscrepancyId { get; init; }

    public OperationsDiscrepancyStatus Outcome { get; init; }

    public string? Note { get; init; }

    public string? PortalSlug { get; init; }
}
