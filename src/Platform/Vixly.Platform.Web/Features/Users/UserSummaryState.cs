using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Users;

// =========================================================
// FILE: UserSummaryState.cs
//
// PURPOSE:
// What the user summary shows.
//
// RESPONSIBILITIES:
// - Carry the user detail, or the error
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record UserSummaryState(PlatformUserDetail? User, Error? Error)
{
    public static UserSummaryState Initial { get; } = new(null, null);
}
