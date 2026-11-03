using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.MyAccount.GetStepUpOptions;

// =========================================================
// FILE: GetStepUpOptionsResponse.cs
//
// PURPOSE:
// How the signed-in person may confirm a sensitive action: with the password only, or with a second factor.
//
// RESPONSIBILITIES:
// - Carry the values named above, nothing more
//
// IMPORTANT NOTES:
// - Part of Require MFA (owner, 2026-10-22)
// =========================================================
public sealed class GetStepUpOptionsResponse
{
    private GetStepUpOptionsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public bool PasswordOnly { get; private init; }

    public static GetStepUpOptionsResponse Success(bool passwordOnly)
    {
        return new GetStepUpOptionsResponse { Succeeded = true, PasswordOnly = passwordOnly };
    }

    public static GetStepUpOptionsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetStepUpOptionsResponse { Error = error };
    }
}
