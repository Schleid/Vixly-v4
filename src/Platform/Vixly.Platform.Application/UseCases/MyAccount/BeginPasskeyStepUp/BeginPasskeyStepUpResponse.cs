using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.MyAccount.BeginPasskeyStepUp;

// =========================================================
// FILE: BeginPasskeyStepUpResponse.cs
//
// PURPOSE:
// Result of BeginPasskeyStepUp.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class BeginPasskeyStepUpResponse
{
    private BeginPasskeyStepUpResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string CeremonyToken { get; private init; } = string.Empty;

    public string OptionsJson { get; private init; } = string.Empty;

    public static BeginPasskeyStepUpResponse Success(string ceremonyToken, string optionsJson)
    {
        return new BeginPasskeyStepUpResponse { Succeeded = true, CeremonyToken = ceremonyToken, OptionsJson = optionsJson };
    }

    public static BeginPasskeyStepUpResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new BeginPasskeyStepUpResponse { Error = error };
    }
}
