using Vixly.Platform.Domain.Licensing;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformDeviceEventItem.cs
//
// PURPOSE:
// One line of a device's history, with who did it.
//
// RESPONSIBILITIES:
// - Carry code, reason, note, arguments and the actor name
//
// IMPORTANT NOTES:
// - Tenant actors show the name Operations sent
// =========================================================
public sealed record PlatformDeviceEventItem(
    DateTimeOffset OccurredUtc,
    string EventCode,
    PlatformDeviceReason? Reason,
    string? Note,
    IReadOnlyDictionary<string, string> Arguments,
    string? ActorName);
