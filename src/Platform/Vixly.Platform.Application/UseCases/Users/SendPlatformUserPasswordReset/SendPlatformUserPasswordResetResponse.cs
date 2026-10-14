using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Users.SendPlatformUserPasswordReset;

// =========================================================
// FILE: SendPlatformUserPasswordResetResponse.cs
//
// PURPOSE:
// Result of SendPlatformUserPasswordReset.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SendPlatformUserPasswordResetResponse
{
    private SendPlatformUserPasswordResetResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SendPlatformUserPasswordResetResponse Success()
    {
        return new SendPlatformUserPasswordResetResponse { Succeeded = true };
    }

    public static SendPlatformUserPasswordResetResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SendPlatformUserPasswordResetResponse { Error = error };
    }
}
