using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Audit;

// =========================================================
// FILE: SignInActivityState.cs
//
// PURPOSE:
// What the member activity list shows (owner, 2026-10-21).
//
// RESPONSIBILITIES:
// - Hold one page of sign-ins, the paging numbers and any error
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record SignInActivityState(
    bool Loaded,
    IReadOnlyList<PlatformSignInItem> SignIns,
    int Page,
    int TotalPages,
    int TotalCount,
    Error? Error)
{
    public static SignInActivityState Initial { get; } = new(false, Array.Empty<PlatformSignInItem>(), 1, 0, 0, null);
}
