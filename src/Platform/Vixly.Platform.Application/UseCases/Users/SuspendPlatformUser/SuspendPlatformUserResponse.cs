using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Users.SuspendPlatformUser;

// =========================================================
// FILE: SuspendPlatformUserResponse.cs
//
// PURPOSE:
// Result of SuspendPlatformUser.
//
// RESPONSIBILITIES:
// - Report success, or the error code to show
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SuspendPlatformUserResponse
{
    private SuspendPlatformUserResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SuspendPlatformUserResponse Success()
    {
        return new SuspendPlatformUserResponse { Succeeded = true };
    }

    public static SuspendPlatformUserResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SuspendPlatformUserResponse { Error = error };
    }
}
