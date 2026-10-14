using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Account;

// =========================================================
// FILE: MySessionsState.cs
//
// PURPOSE:
// What the sessions card of My account shows.
//
// RESPONSIBILITIES:
// - Carry the open sessions, which one is this device, the error
//   and the last message
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record MySessionsState(
    IReadOnlyList<PlatformUserSessionItem> Sessions,
    Guid? CurrentSessionId,
    Error? Error,
    int? EndedCount)
{
    public static MySessionsState Initial { get; } = new(Array.Empty<PlatformUserSessionItem>(), null, null, null);
}
