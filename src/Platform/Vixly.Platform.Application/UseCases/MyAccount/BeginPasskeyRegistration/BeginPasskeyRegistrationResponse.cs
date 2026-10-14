using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.MyAccount.BeginPasskeyRegistration;

// =========================================================
// FILE: BeginPasskeyRegistrationResponse.cs
//
// PURPOSE:
// Result of BeginPasskeyRegistration.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class BeginPasskeyRegistrationResponse
{
    private BeginPasskeyRegistrationResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string CeremonyToken { get; private init; } = string.Empty;

    public string OptionsJson { get; private init; } = string.Empty;

    public static BeginPasskeyRegistrationResponse Success(string ceremonyToken, string optionsJson)
    {
        return new BeginPasskeyRegistrationResponse { Succeeded = true, CeremonyToken = ceremonyToken, OptionsJson = optionsJson };
    }

    public static BeginPasskeyRegistrationResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new BeginPasskeyRegistrationResponse { Error = error };
    }
}
