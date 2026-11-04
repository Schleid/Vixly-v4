using Vixly.Platform.Domain.Sales;

namespace Vixly.Platform.Application.UseCases.Leads.SubmitPlatformSalesLead;

// =========================================================
// FILE: SubmitPlatformSalesLeadRequest.cs
//
// PURPOSE:
// A visitor of the public site asks Vixly sales to get in touch (Stage 2, D-U3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SubmitPlatformSalesLeadRequest
{
    public string? CompanyName { get; init; }

    public string? ContactName { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public string? CountryCode { get; init; }

    public PlatformSalesLeadKind Kind { get; init; }

    public string? Message { get; init; }

    public string? Language { get; init; }

    public string? SourcePage { get; init; }

    public string? BotToken { get; init; }
}
