namespace Vixly.Contracts.Platform.Internal.Entitlements;

// =========================================================
// FILE: PlatformEntitlementsInfo.cs
//
// PURPOSE:
// What a tenant may use now (call 14).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Access: full, read_only, payment_required, none; unknown features and limits are ignored by consumers
// - Cache 5 minutes; drop on subscription, plan and legal events (D-K6)
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformEntitlementsInfo(
    Guid TenantId,
    string? Status,
    string Access,
    IReadOnlyList<string> Features,
    IReadOnlyDictionary<string, int> Limits,
    PlatformNonPaymentPartInfo? NonPayment,
    PlatformEntitlementsLegalInfo? Legal);
