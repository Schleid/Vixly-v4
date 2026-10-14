using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.MyAccount.ConfirmSecondFactor;

// =========================================================
// FILE: ConfirmSecondFactorResponse.cs
//
// PURPOSE:
// Result of ConfirmSecondFactor.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// - platform.auth.session_ended means the session is over:
//   the presenter sends the user to sign in again
// =========================================================
public sealed class ConfirmSecondFactorResponse
{
    private ConfirmSecondFactorResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ConfirmSecondFactorResponse Success()
    {
        return new ConfirmSecondFactorResponse { Succeeded = true };
    }

    public static ConfirmSecondFactorResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ConfirmSecondFactorResponse { Error = error };
    }
}
