namespace Vixly.Operations.Application.Platform;

// =========================================================
// FILE: OperationsPortalAnswer.cs
//
// PURPOSE:
// What Platform says about a host name (Stage 3, Slice 0, D-F2).
//
// RESPONSIBILITIES:
// - Carry Platform's answer in Operations terms
//
// IMPORTANT NOTES:
// - Status and access are Platform's lowercase wire codes (ADR-0014)
// - Found is false for unknown, deactivated and banned tenants
// =========================================================
public sealed record OperationsPortalAnswer(bool Found, Guid? TenantId, string? Status, string Access, string? CanonicalSlug, string? DefaultLanguage, string Legal);
