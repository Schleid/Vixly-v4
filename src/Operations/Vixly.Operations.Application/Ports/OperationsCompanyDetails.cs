namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsCompanyDetails.cs
//
// PURPOSE:
// The company's legal details: changed only by a request with proof (B12).
//
// RESPONSIBILITIES:
// - Carry what Platform answered
//
// IMPORTANT NOTES:
// - Read-only; mapped from Contracts.Platform in Infrastructure
// =========================================================
public sealed record OperationsCompanyDetails(string LegalName, string? TradingName, string Email, string Phone, OperationsCompanyAddress Address);
