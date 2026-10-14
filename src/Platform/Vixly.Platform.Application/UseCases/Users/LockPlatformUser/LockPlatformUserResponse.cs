using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Users.LockPlatformUser;

// =========================================================
// FILE: LockPlatformUserResponse.cs
//
// PURPOSE:
// Result of LockPlatformUser.
//
// RESPONSIBILITIES:
// - Report success, or the error code to show
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class LockPlatformUserResponse
{
    private LockPlatformUserResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static LockPlatformUserResponse Success()
    {
        return new LockPlatformUserResponse { Succeeded = true };
    }

    public static LockPlatformUserResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new LockPlatformUserResponse { Error = error };
    }
}
