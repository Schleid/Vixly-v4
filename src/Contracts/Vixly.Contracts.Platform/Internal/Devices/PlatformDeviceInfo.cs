namespace Vixly.Contracts.Platform.Internal.Devices;

// =========================================================
// FILE: PlatformDeviceInfo.cs
//
// PURPOSE:
// One of the tenant's devices (call 24).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformDeviceInfo(
    Guid DeviceId,
    string Number,
    string Name,
    string Kind,
    string Proof,
    string Status,
    bool SeatHeld,
    string? Reason,
    string? RequestedByName,
    DateTimeOffset RequestedUtc,
    DateTimeOffset? ApprovedUtc,
    DateTimeOffset? ReleaseRequestedUtc,
    DateTimeOffset? EndedUtc,
    DateTimeOffset? LastSeenUtc,
    bool Idle);
