using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Users.DeactivatePlatformUser;

// =========================================================
// FILE: DeactivatePlatformUserResponse.cs
//
// PURPOSE:
// Result of DeactivatePlatformUser.
//
// RESPONSIBILITIES:
// - Report success, or the error code to show
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class DeactivatePlatformUserResponse
{
    private DeactivatePlatformUserResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static DeactivatePlatformUserResponse Success()
    {
        return new DeactivatePlatformUserResponse { Succeeded = true };
    }

    public static DeactivatePlatformUserResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new DeactivatePlatformUserResponse { Error = error };
    }
}
