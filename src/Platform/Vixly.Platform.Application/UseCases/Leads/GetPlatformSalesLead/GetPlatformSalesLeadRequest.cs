namespace Vixly.Platform.Application.UseCases.Leads.GetPlatformSalesLead;

// =========================================================
// FILE: GetPlatformSalesLeadRequest.cs
//
// PURPOSE:
// One contact request, and whether the reader may update it (Stage 2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPlatformSalesLeadRequest
{
    public Guid LeadId { get; init; }
}
