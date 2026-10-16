namespace Vixly.Platform.Application.UseCases.Licensing.CreatePlatformDeviceChallenge;

// =========================================================
// FILE: CreatePlatformDeviceChallengeRequest.cs
//
// PURPOSE:
// Gives a key-pair device a fresh challenge to sign (D-L5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CreatePlatformDeviceChallengeRequest
{
    public Guid DeviceId { get; init; }
}
