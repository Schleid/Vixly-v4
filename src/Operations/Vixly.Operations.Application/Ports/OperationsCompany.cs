namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsCompany.cs
//
// PURPOSE:
// The company's account at Vixly as Operations shows it (Slice 1E).
//
// RESPONSIBILITIES:
// - Carry what Platform answered
//
// IMPORTANT NOTES:
// - Read-only; mapped from Contracts.Platform in Infrastructure
// =========================================================
public sealed record OperationsCompany(string TenantCode, OperationsCompanyDetails Details, string DefaultLanguage, string TimeZone, IReadOnlyList<OperationsCompanyContact> Contacts);
