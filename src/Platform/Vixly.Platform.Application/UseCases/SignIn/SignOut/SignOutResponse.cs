using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.SignIn.SignOut;

// =========================================================
// FILE: SignOutResponse.cs
//
// PURPOSE:
// Result of SignOut.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Always succeeds for a signed-in caller
// =========================================================
public sealed class SignOutResponse
{
    private SignOutResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SignOutResponse Success()
    {
        return new SignOutResponse { Succeeded = true };
    }

    public static SignOutResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SignOutResponse { Error = error };
    }
}
