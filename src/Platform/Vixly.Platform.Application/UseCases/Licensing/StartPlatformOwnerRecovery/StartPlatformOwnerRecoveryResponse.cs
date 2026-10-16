using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Licensing.StartPlatformOwnerRecovery;

// =========================================================
// FILE: StartPlatformOwnerRecoveryResponse.cs
//
// PURPOSE:
// Vixly staff email the owner a device recovery link (D-L8).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class StartPlatformOwnerRecoveryResponse
{
    private StartPlatformOwnerRecoveryResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static StartPlatformOwnerRecoveryResponse Success()
    {
        return new StartPlatformOwnerRecoveryResponse { Succeeded = true };
    }

    public static StartPlatformOwnerRecoveryResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new StartPlatformOwnerRecoveryResponse { Error = error };
    }
}
