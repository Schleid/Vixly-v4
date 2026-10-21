using Vixly.Platform.Domain.Sales;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformSalesLeadItem.cs
//
// PURPOSE:
// One contact request in the console list and on its page.
//
// RESPONSIBILITIES:
// - Carry what staff see
//
// IMPORTANT NOTES:
// - The client IP is not shown
// =========================================================
public sealed record PlatformSalesLeadItem(
    Guid LeadId,
    string CompanyName,
    string ContactName,
    string Email,
    string? Phone,
    string? CountryCode,
    PlatformSalesLeadKind Kind,
    string? Message,
    string Language,
    string? SourcePage,
    PlatformSalesLeadStatus Status,
    DateTimeOffset ReceivedUtc,
    DateTimeOffset StatusChangedUtc,
    string? StaffNote,
    string? HandledByName);
