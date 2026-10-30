namespace Vixly.Contracts.Platform.Internal.Tenant;

// =========================================================
// FILE: PlatformCompanyDetailsInfo.cs
//
// PURPOSE:
// A company’s legal details on the wire (B12).
//
// RESPONSIBILITIES:
// - Carry the details as Operations shows them
//
// IMPORTANT NOTES:
// - Read model
// =========================================================
public sealed record PlatformCompanyDetailsInfo(string LegalName, string? TradingName, string Email, string Phone, PlatformPostalAddressInfo Address);
