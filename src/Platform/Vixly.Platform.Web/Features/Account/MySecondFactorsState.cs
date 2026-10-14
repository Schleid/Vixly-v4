using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Account;

// =========================================================
// FILE: MySecondFactorsState.cs
//
// PURPOSE:
// What the sign-in methods card of My account shows.
//
// RESPONSIBILITIES:
// - Carry the account, the last message, what waits for a
//   step-up (adding, removing which passkey, new recovery
//   codes) and new recovery codes to show once
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// - InfoKey is a resource key, never text (UI-10)
// =========================================================
public sealed record MySecondFactorsState(
    PlatformUserDetail? Account,
    Error? Error,
    string? InfoKey,
    bool AddWaitsForStepUp,
    Guid? RemovalWaitsForStepUp,
    bool CodesWaitForStepUp,
    IReadOnlyList<string> NewRecoveryCodes)
{
    public static MySecondFactorsState Initial { get; } = new(null, null, null, false, null, false, Array.Empty<string>());

    public bool StepUpPending => AddWaitsForStepUp || RemovalWaitsForStepUp is not null || CodesWaitForStepUp;
}
