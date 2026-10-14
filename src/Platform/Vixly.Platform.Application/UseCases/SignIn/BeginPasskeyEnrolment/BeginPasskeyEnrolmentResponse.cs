using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.SignIn.BeginPasskeyEnrolment;

// =========================================================
// FILE: BeginPasskeyEnrolmentResponse.cs
//
// PURPOSE:
// Result of BeginPasskeyEnrolment.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class BeginPasskeyEnrolmentResponse
{
    private BeginPasskeyEnrolmentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string CeremonyToken { get; private init; } = string.Empty;

    public string OptionsJson { get; private init; } = string.Empty;

    public static BeginPasskeyEnrolmentResponse Success(string ceremonyToken, string optionsJson)
    {
        return new BeginPasskeyEnrolmentResponse { Succeeded = true, CeremonyToken = ceremonyToken, OptionsJson = optionsJson };
    }

    public static BeginPasskeyEnrolmentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new BeginPasskeyEnrolmentResponse { Error = error };
    }
}
