using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Licensing.StartPlatformOwnerRecoveryForOwner;

// =========================================================
// FILE: StartPlatformOwnerRecoveryForOwnerResponse.cs
//
// PURPOSE:
// The owner, confirmed by email in Operations, asks for a device recovery link (D-L8).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class StartPlatformOwnerRecoveryForOwnerResponse
{
    private StartPlatformOwnerRecoveryForOwnerResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static StartPlatformOwnerRecoveryForOwnerResponse Success()
    {
        return new StartPlatformOwnerRecoveryForOwnerResponse { Succeeded = true };
    }

    public static StartPlatformOwnerRecoveryForOwnerResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new StartPlatformOwnerRecoveryForOwnerResponse { Error = error };
    }
}
