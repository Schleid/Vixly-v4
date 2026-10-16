using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformLegalRegisterItem.cs
//
// PURPOSE:
// One tenant in the acceptance register.
//
// RESPONSIBILITIES:
// - Carry the tenant, its legal status, deadline, acceptance and last reminder
//
// IMPORTANT NOTES:
// - Read model
// =========================================================
public sealed record PlatformLegalRegisterItem(
    Guid PlatformTenantId,
    string TenantName,
    string TenantCode,
    PlatformTenantStatus TenantStatus,
    PlatformLegalStatus Status,
    DateTimeOffset DeadlineUtc,
    DateTimeOffset? AcceptedUtc,
    string? AcceptedVersion,
    DateTimeOffset? LastReminderUtc);
