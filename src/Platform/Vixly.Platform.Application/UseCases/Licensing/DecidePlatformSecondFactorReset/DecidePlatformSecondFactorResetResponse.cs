using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Licensing.DecidePlatformSecondFactorReset;

// =========================================================
// FILE: DecidePlatformSecondFactorResetResponse.cs
//
// PURPOSE:
// A second Vixly staff member approves or refuses a reset of the Owner's second factor (D-O13).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class DecidePlatformSecondFactorResetResponse
{
    private DecidePlatformSecondFactorResetResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static DecidePlatformSecondFactorResetResponse Success()
    {
        return new DecidePlatformSecondFactorResetResponse { Succeeded = true };
    }

    public static DecidePlatformSecondFactorResetResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new DecidePlatformSecondFactorResetResponse { Error = error };
    }
}
