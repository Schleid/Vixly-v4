namespace Vixly.Operations.Application.Audit;

// =========================================================
// FILE: OperationsActivityLine.cs
//
// PURPOSE:
// One sign-in of a member: when, where, on which device, and
// how it ended (owner, 2026-10-21: member activity).
//
// RESPONSIBILITIES:
// - Carry the values the activity page shows
//
// IMPORTANT NOTES:
// - Ended: the reason as stored (signed_out, idle_timeout...);
//   none while the session may still be in use
// - CountryChanged: the device signed in from another country
//   than the one it was registered in (confirmed by a second factor)
// =========================================================
public sealed record OperationsActivityLine(
    Guid SessionId,
    Guid UserId,
    string? MemberName,
    DateTimeOffset SignedInUtc,
    DateTimeOffset? SignedOutUtc,
    string? Ended,
    string? DeviceName,
    string? DeviceNumber,
    string? Browser,
    string? Country,
    bool CountryChanged);
