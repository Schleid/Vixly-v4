using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.ChangePlatformSetupFee;

// =========================================================
// FILE: ChangePlatformSetupFeeResponse.cs
//
// PURPOSE:
// Changes or waives the setup fee for one tenant, before it is charged (D-S23).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangePlatformSetupFeeResponse
{
    private ChangePlatformSetupFeeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ChangePlatformSetupFeeResponse Success()
    {
        return new ChangePlatformSetupFeeResponse { Succeeded = true };
    }

    public static ChangePlatformSetupFeeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangePlatformSetupFeeResponse { Error = error };
    }
}
