using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Users.ReactivatePlatformUser;

// =========================================================
// FILE: ReactivatePlatformUserResponse.cs
//
// PURPOSE:
// Result of ReactivatePlatformUser.
//
// RESPONSIBILITIES:
// - Report success, or the error code to show
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ReactivatePlatformUserResponse
{
    private ReactivatePlatformUserResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ReactivatePlatformUserResponse Success()
    {
        return new ReactivatePlatformUserResponse { Succeeded = true };
    }

    public static ReactivatePlatformUserResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ReactivatePlatformUserResponse { Error = error };
    }
}
