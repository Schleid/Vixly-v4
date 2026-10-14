using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformTenantSuspensionItem.cs
//
// PURPOSE:
// The open suspension of a tenant.
//
// RESPONSIBILITIES:
// - Carry read-only data to the console
//
// IMPORTANT NOTES:
// - The reason is internal: shown to staff only
// =========================================================
public sealed record PlatformTenantSuspensionItem(
    PlatformSuspensionCategory Category,
    string Reason,
    string? SuspendedByName,
    DateTimeOffset SuspendedUtc);
