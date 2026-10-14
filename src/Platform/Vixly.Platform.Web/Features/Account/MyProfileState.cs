using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Account;

// =========================================================
// FILE: MyProfileState.cs
//
// PURPOSE:
// What the profile card of My account shows.
//
// RESPONSIBILITIES:
// - Carry the account, the error, and whether the language just
//   changed
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record MyProfileState(PlatformUserDetail? Account, Error? Error, bool LanguageChanged)
{
    public static MyProfileState Initial { get; } = new(null, null, false);
}
