namespace Vixly.Operations.Application.Platform;

// =========================================================
// FILE: OperationsTenantAccount.cs
//
// PURPOSE:
// What Platform says about a tenant account (GET /tenant), in Operations terms.
//
// RESPONSIBILITIES:
// - Carry names, language and the owner contact
//
// IMPORTANT NOTES:
// - Null owner fields when Platform has no owner contact
// =========================================================
public sealed record OperationsTenantAccount(
    Guid TenantId,
    string Slug,
    string LegalName,
    string? TradingName,
    string DefaultLanguage,
    string? OwnerName,
    string? OwnerEmail)
{
    public string DisplayName => string.IsNullOrWhiteSpace(TradingName) ? LegalName : TradingName;
}
