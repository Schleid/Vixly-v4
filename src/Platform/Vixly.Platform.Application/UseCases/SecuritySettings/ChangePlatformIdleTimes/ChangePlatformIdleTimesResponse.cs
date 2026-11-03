using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.SecuritySettings.ChangePlatformIdleTimes;

// =========================================================
// FILE: ChangePlatformIdleTimesResponse.cs
//
// PURPOSE:
// Whether Control's inactivity times were changed.
//
// RESPONSIBILITIES:
// - Carry success or the refusal
//
// IMPORTANT NOTES:
// - Step-up needed: PlatformCommonErrors.StepUpRequired
// =========================================================
public sealed class ChangePlatformIdleTimesResponse
{
    private ChangePlatformIdleTimesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ChangePlatformIdleTimesResponse Success() => new() { Succeeded = true };

    public static ChangePlatformIdleTimesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new ChangePlatformIdleTimesResponse { Error = error };
    }
}
