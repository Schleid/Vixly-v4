using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Licensing.VerifyPlatformDevice;

// =========================================================
// FILE: VerifyPlatformDeviceUseCase.cs
//
// PURPOSE:
// Answers whether a device may be used now, from its signature or its rotating secret (D-L5, D-L10).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Called by Operations over the internal channel (Slice 9) for the tenant in the caller context only (S-13)
// - Operations has checked its own permission and step-up for the acting user
// - Answers carry codes only; Operations may cache a positive answer for 5 minutes
// =========================================================
public sealed class VerifyPlatformDeviceUseCase
{
    private readonly PlatformLicensingAdministration _licensing;

    public VerifyPlatformDeviceUseCase(
        PlatformLicensingAdministration licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public async Task<VerifyPlatformDeviceResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        VerifyPlatformDeviceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformTenant? tenant = await _licensing.Tenants.GetAsync(context.PlatformTenantId, cancellationToken);

        if (tenant is null || tenant.Status is PlatformTenantStatus.Deactivated or PlatformTenantStatus.Banned or PlatformTenantStatus.Draft or PlatformTenantStatus.PendingActivation)
        {
            return VerifyPlatformDeviceResponse.Success(Refuse("tenant_closed"));
        }

        if (tenant.Status == PlatformTenantStatus.Suspended)
        {
            return VerifyPlatformDeviceResponse.Success(Refuse("tenant_suspended"));
        }

        PlatformDevice? device = await _licensing.DeviceForAsync(context, request.DeviceId, cancellationToken);

        if (device is null)
        {
            return VerifyPlatformDeviceResponse.Success(Refuse("device_unknown"));
        }

        if (device.Status == PlatformDeviceStatus.Pending)
        {
            return VerifyPlatformDeviceResponse.Success(Refuse("device_pending"));
        }

        if (device.Status is PlatformDeviceStatus.Released or PlatformDeviceStatus.Denied or PlatformDeviceStatus.Expired)
        {
            return VerifyPlatformDeviceResponse.Success(Refuse("device_released"));
        }

        if (device.Status == PlatformDeviceStatus.Revoked)
        {
            return VerifyPlatformDeviceResponse.Success(Refuse("device_revoked"));
        }

        DateTimeOffset utcNow = _licensing.UtcNow;
        string? newSecret = null;

        if (device.Proof == PlatformDeviceProof.KeyPair)
        {
            PlatformDeviceChallenge? challenge = string.IsNullOrEmpty(request.Challenge) ? null : await _licensing.Store.GetChallengeAsync(_licensing.Hash(request.Challenge), cancellationToken);

            if (challenge is null || challenge.PlatformDeviceId != device.Id || !challenge.Use(utcNow)
                || !_licensing.Keys.Verify(device.PublicKey!, System.Text.Encoding.UTF8.GetBytes(request.Challenge!), request.Signature))
            {
                await _licensing.SaveAsync(cancellationToken);
                return VerifyPlatformDeviceResponse.Success(Refuse("proof_invalid"));
            }
        }
        else
        {
            PlatformDeviceSecretCheck result = device.CheckSecret(string.IsNullOrEmpty(request.Secret) ? null : _licensing.Hash(request.Secret), utcNow);

            if (result == PlatformDeviceSecretCheck.Reused)
            {
                // A copied old secret: the device can no longer be trusted (D-L5).
                _licensing.Record(device, device.Revoke(PlatformDeviceReason.CredentialReused, PlatformDeviceActor.System, utcNow),
                    await _licensing.SeatsAsync(device.PlatformTenantId, cancellationToken), int.MaxValue, null, "platform.licensing.device_revoked", null);
                _licensing.Notice(device.PlatformTenantId, PlatformLicensingNoticeKind.CredentialReused, device.Id);
                await _licensing.SaveAsync(cancellationToken);
                return VerifyPlatformDeviceResponse.Success(Refuse("device_revoked"));
            }

            if (result == PlatformDeviceSecretCheck.Invalid)
            {
                return VerifyPlatformDeviceResponse.Success(Refuse("proof_invalid"));
            }

            if (result == PlatformDeviceSecretCheck.ValidRotate)
            {
                PlatformSecret rotated = _licensing.NewSecret();
                device.RotateSecret(rotated.Hash, utcNow);
                newSecret = rotated.Value;
            }
        }

        bool touched = device.Touch(utcNow);
        PlatformSubscriptionAccess access = await _licensing.AccessAsync(device.PlatformTenantId, cancellationToken);

        if (access == PlatformSubscriptionAccess.None)
        {
            return VerifyPlatformDeviceResponse.Success(Refuse("subscription_none"));
        }

        if ((touched || newSecret is not null || device.Proof == PlatformDeviceProof.KeyPair) && !await _licensing.SaveAsync(cancellationToken))
        {
            return VerifyPlatformDeviceResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return VerifyPlatformDeviceResponse.Success(new PlatformDeviceCheck(true, null, access, newSecret));
    }

    private static PlatformDeviceCheck Refuse(string reason)
    {
        return new PlatformDeviceCheck(false, reason, PlatformSubscriptionAccess.None, null);
    }
}
