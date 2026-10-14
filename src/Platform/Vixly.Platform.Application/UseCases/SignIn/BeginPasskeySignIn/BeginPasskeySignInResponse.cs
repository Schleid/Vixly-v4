using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.SignIn.BeginPasskeySignIn;

// =========================================================
// FILE: BeginPasskeySignInResponse.cs
//
// PURPOSE:
// Result of BeginPasskeySignIn.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// - The token goes in a short-lived HttpOnly cookie; the options
//   go to the browser
// =========================================================
public sealed class BeginPasskeySignInResponse
{
    private BeginPasskeySignInResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string CeremonyToken { get; private init; } = string.Empty;

    public string OptionsJson { get; private init; } = string.Empty;

    public static BeginPasskeySignInResponse Success(string ceremonyToken, string optionsJson)
    {
        return new BeginPasskeySignInResponse { Succeeded = true, CeremonyToken = ceremonyToken, OptionsJson = optionsJson };
    }

    public static BeginPasskeySignInResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new BeginPasskeySignInResponse { Error = error };
    }
}
