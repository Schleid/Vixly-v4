namespace Vixly.Platform.Application.UseCases.TenantAccount;

// =========================================================
// FILE: PlatformVixlySupport.cs
//
// PURPOSE:
// How the tenant reaches Vixly support.
//
// RESPONSIBILITIES:
// - Carry what Operations shows the tenant
//
// IMPORTANT NOTES:
// - Plain values for the wire mapping (D-K10)
// =========================================================
public sealed record PlatformVixlySupport(string CompanyName, string? Email, string? Phone, string? Website);
