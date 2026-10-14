using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformTenantStatusChanged.cs
//
// PURPOSE:
// The payload of platform.tenant.status_changed.v1.
//
// RESPONSIBILITIES:
// - Say which tenant moved, from what to what, and when
//
// IMPORTANT NOTES:
// - No reason: handlers and other planes never see it
// - Slice 3 reads this to suspend or restore the subscription
// =========================================================
public sealed record PlatformTenantStatusChanged(
    Guid PlatformTenantId,
    string TenantCode,
    PlatformTenantStatus? FromStatus,
    PlatformTenantStatus ToStatus,
    PlatformSuspensionCategory? Category,
    DateTimeOffset OccurredUtc);
