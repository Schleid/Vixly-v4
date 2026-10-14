using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Users.ResetPlatformUserSecondFactor;

// =========================================================
// FILE: ResetPlatformUserSecondFactorResponse.cs
//
// PURPOSE:
// Result of ResetPlatformUserSecondFactor.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ResetPlatformUserSecondFactorResponse
{
    private ResetPlatformUserSecondFactorResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ResetPlatformUserSecondFactorResponse Success()
    {
        return new ResetPlatformUserSecondFactorResponse { Succeeded = true };
    }

    public static ResetPlatformUserSecondFactorResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ResetPlatformUserSecondFactorResponse { Error = error };
    }
}
