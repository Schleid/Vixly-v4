using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Licensing.CreatePlatformDeviceChallenge;

// =========================================================
// FILE: CreatePlatformDeviceChallengeUseCase.cs
//
// PURPOSE:
// Gives a key-pair device a fresh challenge to sign (D-L5).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Called by Operations over the internal channel (Slice 9) for the tenant in the caller context only (S-13)
// - Operations has checked its own permission and step-up for the acting user
// - The challenge is single use and lives 2 minutes
// =========================================================
public sealed class CreatePlatformDeviceChallengeUseCase
{
    private readonly PlatformLicensingAdministration _licensing;

    public CreatePlatformDeviceChallengeUseCase(
        PlatformLicensingAdministration licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public async Task<CreatePlatformDeviceChallengeResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        CreatePlatformDeviceChallengeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformDevice? device = await _licensing.DeviceForAsync(context, request.DeviceId, cancellationToken);

        if (device is null || device.Proof != PlatformDeviceProof.KeyPair || !device.HoldsSeat || device.Status == PlatformDeviceStatus.Revoked)
        {
            return CreatePlatformDeviceChallengeResponse.Failure(PlatformLicensingErrors.DeviceNotFound);
        }

        PlatformSecret challenge = _licensing.NewSecret();
        _licensing.Store.AddChallenge(PlatformDeviceChallenge.Create(device.Id, challenge.Hash, _licensing.UtcNow));

        return await _licensing.SaveAsync(cancellationToken)
            ? CreatePlatformDeviceChallengeResponse.Success(challenge.Value)
            : CreatePlatformDeviceChallengeResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
