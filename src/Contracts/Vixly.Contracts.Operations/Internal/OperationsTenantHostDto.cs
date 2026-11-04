namespace Vixly.Contracts.Operations.Internal;

// =========================================================
// FILE: OperationsTenantHostDto.cs
//
// PURPOSE:
// Which tenant a host name belongs to, as Operations.Api tells Customer.Web (Stage 3, Slice 0).
//
// RESPONSIBILITIES:
// - Carry the tenant, its state and its language
//
// IMPORTANT NOTES:
// - State is a lowercase code: not_found, live, setting_up, suspended
// - Access is a lowercase code: full, read_only, payment_required, none
// - Additive changes only (cross-plane rule 4)
// =========================================================
public sealed record OperationsTenantHostDto(bool Found, Guid? TenantId, string State, string Access, string? CanonicalSlug, string? DefaultLanguage);
