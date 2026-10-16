using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.UseCases.TenantAccount;

// =========================================================
// FILE: PlatformTenantAccountContact.cs
//
// PURPOSE:
// One tenant contact.
//
// RESPONSIBILITIES:
// - Carry what Operations shows the tenant
//
// IMPORTANT NOTES:
// - Plain values for the wire mapping (D-K10)
// =========================================================
public sealed record PlatformTenantAccountContact(PlatformTenantContactRole Role, string Name, string Email, string? Phone);
