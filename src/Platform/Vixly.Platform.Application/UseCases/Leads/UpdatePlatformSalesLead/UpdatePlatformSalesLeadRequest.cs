using Vixly.Platform.Domain.Sales;

namespace Vixly.Platform.Application.UseCases.Leads.UpdatePlatformSalesLead;

// =========================================================
// FILE: UpdatePlatformSalesLeadRequest.cs
//
// PURPOSE:
// Sales staff mark a contact request as contacted or closed, with a note (Stage 2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class UpdatePlatformSalesLeadRequest
{
    public Guid LeadId { get; init; }

    public PlatformSalesLeadStatus Status { get; init; }

    public string? Note { get; init; }
}
