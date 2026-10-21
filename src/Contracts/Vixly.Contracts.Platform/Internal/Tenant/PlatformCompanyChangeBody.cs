namespace Vixly.Contracts.Platform.Internal.Tenant;

// =========================================================
// FILE: PlatformCompanyChangeBody.cs
//
// PURPOSE:
// The tenant owner’s request to change the company’s legal details, sent by Operations (B12).
//
// RESPONSIBILITIES:
// - Carry the new details, the reason and the proof document
//
// IMPORTANT NOTES:
// - The proof travels as base64: PDF, JPEG or PNG, at most 10 MB
// - The whole set of details is sent, changed or not
// =========================================================
public sealed record PlatformCompanyChangeBody
{
    public string LegalName { get; init; } = string.Empty;

    public string? TradingName { get; init; }

    public string Email { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;

    public string AddressLine1 { get; init; } = string.Empty;

    public string? AddressLine2 { get; init; }

    public string City { get; init; } = string.Empty;

    public string? StateProvince { get; init; }

    public string? PostalCode { get; init; }

    public string CountryCode { get; init; } = string.Empty;

    public string? Reason { get; init; }

    public string? ProofFileName { get; init; }

    public string? ProofBase64 { get; init; }
}
