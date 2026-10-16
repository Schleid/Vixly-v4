using Vixly.Platform.Domain.Licensing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Licensing.WithdrawPlatformDeviceRelease;

// =========================================================
// FILE: WithdrawPlatformDeviceReleaseResponse.cs
//
// PURPOSE:
// A tenant admin withdraws a release still waiting for Vixly (D-L6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class WithdrawPlatformDeviceReleaseResponse
{
    private WithdrawPlatformDeviceReleaseResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static WithdrawPlatformDeviceReleaseResponse Success()
    {
        return new WithdrawPlatformDeviceReleaseResponse { Succeeded = true };
    }

    public static WithdrawPlatformDeviceReleaseResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new WithdrawPlatformDeviceReleaseResponse { Error = error };
    }
}
