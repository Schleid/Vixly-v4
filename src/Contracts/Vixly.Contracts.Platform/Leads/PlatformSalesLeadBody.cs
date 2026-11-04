namespace Vixly.Contracts.Platform.Leads;

// =========================================================
// FILE: PlatformSalesLeadBody.cs
//
// PURPOSE:
// The public contact form (Stage 2, D-U3).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Public.Web and Platform.Api
//
// IMPORTANT NOTES:
// - Kind: courier, freight, warehouse, multi_branch, other
// - BotToken is the Turnstile token of the form
// - Validated by Platform; nothing here is trusted
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record PlatformSalesLeadBody
{
    public string CompanyName { get; init; } = string.Empty;

    public string ContactName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string? Phone { get; init; }

    public string? CountryCode { get; init; }

    public string Kind { get; init; } = "other";

    public string? Message { get; init; }

    public string Language { get; init; } = "en";

    public string? SourcePage { get; init; }

    public string? BotToken { get; init; }
}
