using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformTenantEventItem.cs
//
// PURPOSE:
// One entry of a tenant's history.
//
// RESPONSIBILITIES:
// - Carry read-only data to the console
//
// IMPORTANT NOTES:
// - The code and arguments are turned into text by the console
//   in the reader's language (D-4)
// =========================================================
public sealed record PlatformTenantEventItem(
    Guid EventId,
    string EventCode,
    PlatformTenantStatus? FromStatus,
    PlatformTenantStatus ToStatus,
    PlatformSuspensionCategory? Category,
    string? Reason,
    string? ActorName,
    IReadOnlyDictionary<string, string> Arguments,
    DateTimeOffset OccurredUtc);
