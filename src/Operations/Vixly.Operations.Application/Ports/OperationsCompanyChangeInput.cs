namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsCompanyChangeInput.cs
//
// PURPOSE:
// A request to change the legal details, with its proof (B12).
//
// RESPONSIBILITIES:
// - Carry the proposed details, the reason and the proof file
//
// IMPORTANT NOTES:
// - Platform checks the proof type and size
// =========================================================
public sealed class OperationsCompanyChangeInput
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

    public string Reason { get; init; } = string.Empty;

    public string? ProofFileName { get; init; }

    public byte[]? ProofContent { get; init; }
}
