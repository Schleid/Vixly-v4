namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsCompanyAddress.cs
//
// PURPOSE:
// The company's postal address at Vixly (Platform).
//
// RESPONSIBILITIES:
// - Carry what Platform answered
//
// IMPORTANT NOTES:
// - Read-only; mapped from Contracts.Platform in Infrastructure
// =========================================================
public sealed record OperationsCompanyAddress(string Line1, string? Line2, string City, string? StateProvince, string? PostalCode, string CountryCode);
