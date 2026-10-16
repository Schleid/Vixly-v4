using Vixly.Platform.Domain.Licensing;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformDeviceItem.cs
//
// PURPOSE:
// One device in a list or on its page.
//
// RESPONSIBILITIES:
// - Carry what lists and the device page show
//
// IMPORTANT NOTES:
// - Idle: not seen for 60 days; never released automatically
// =========================================================
public sealed record PlatformDeviceItem(
    Guid DeviceId,
    Guid PlatformTenantId,
    string TenantName,
    string Number,
    string Name,
    PlatformDeviceKind Kind,
    PlatformDeviceProof Proof,
    PlatformDeviceStatus Status,
    bool SeatHeld,
    PlatformDeviceReason? Reason,
    string? RequestedByName,
    DateTimeOffset RequestedUtc,
    DateTimeOffset? ApprovedUtc,
    DateTimeOffset? ReleaseRequestedUtc,
    DateTimeOffset? EndedUtc,
    DateTimeOffset? LastSeenUtc,
    bool Idle);
