using Vixly.Platform.Domain.Sales;

namespace Vixly.Platform.Application.UseCases.Leads.ListPlatformSalesLeads;

// =========================================================
// FILE: ListPlatformSalesLeadsRequest.cs
//
// PURPOSE:
// Contact requests for sales staff, newest first (Stage 2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListPlatformSalesLeadsRequest
{
    public PlatformSalesLeadStatus? Status { get; init; }

    public string? Search { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
