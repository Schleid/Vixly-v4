using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.MyAccount.ConfirmSecondFactorWithPasskey;

// =========================================================
// FILE: ConfirmSecondFactorWithPasskeyResponse.cs
//
// PURPOSE:
// Result of ConfirmSecondFactorWithPasskey.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// - platform.auth.session_ended means the session is over
// =========================================================
public sealed class ConfirmSecondFactorWithPasskeyResponse
{
    private ConfirmSecondFactorWithPasskeyResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ConfirmSecondFactorWithPasskeyResponse Success()
    {
        return new ConfirmSecondFactorWithPasskeyResponse { Succeeded = true };
    }

    public static ConfirmSecondFactorWithPasskeyResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ConfirmSecondFactorWithPasskeyResponse { Error = error };
    }
}
