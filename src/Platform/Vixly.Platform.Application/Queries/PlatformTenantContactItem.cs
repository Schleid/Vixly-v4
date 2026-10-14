using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformTenantContactItem.cs
//
// PURPOSE:
// A tenant contact as the console shows it.
//
// RESPONSIBILITIES:
// - Carry read-only data to the console
//
// IMPORTANT NOTES:
// - Phone is optional
// =========================================================
public sealed record PlatformTenantContactItem(
    PlatformTenantContactRole Role,
    string Name,
    string Email,
    string? Phone);
