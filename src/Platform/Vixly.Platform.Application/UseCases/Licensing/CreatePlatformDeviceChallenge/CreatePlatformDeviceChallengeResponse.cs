using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Licensing.CreatePlatformDeviceChallenge;

// =========================================================
// FILE: CreatePlatformDeviceChallengeResponse.cs
//
// PURPOSE:
// Gives a key-pair device a fresh challenge to sign (D-L5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CreatePlatformDeviceChallengeResponse
{
    private CreatePlatformDeviceChallengeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string Challenge { get; private init; } = string.Empty;

    public static CreatePlatformDeviceChallengeResponse Success(string challenge)
    {
        return new CreatePlatformDeviceChallengeResponse { Succeeded = true, Challenge = challenge };
    }

    public static CreatePlatformDeviceChallengeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CreatePlatformDeviceChallengeResponse { Error = error };
    }
}
