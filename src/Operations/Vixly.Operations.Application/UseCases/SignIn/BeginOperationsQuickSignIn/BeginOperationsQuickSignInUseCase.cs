using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Application.UseCases.SignIn.BeginOperationsQuickSignIn;

// =========================================================
// FILE: BeginOperationsQuickSignInUseCase.cs
//
// PURPOSE:
// Signs a staff member in with their badge or PIN on a licensed device of the company (Slice 12A, owner H-8).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - A badge or PIN alone never passes a device that is blocked or used from another country (R-6)
// - The first sign-in of the day on the device asks the authenticator, or the password when the person has none (owner H-8)
// - A wrong PIN and an unknown badge give the same answer; five wrong PINs lock the PIN until a full sign-in
// =========================================================
public sealed class BeginOperationsQuickSignInUseCase
{
    private readonly IOperationsQuickSignInRepository _quick;

    private readonly IOperationsUserRepository _users;

    private readonly IOperationsPasswordHasher _hasher;

    private readonly IOperationsSecretGenerator _secrets;

    private readonly OperationsSignInSteps _steps;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly IOperationsDeviceClient _devices;

    private readonly OperationsAccess _access;

    private readonly OperationsDeviceBlocking _blocking;

    private readonly IOperationsSettingsRepository _settings;

    public BeginOperationsQuickSignInUseCase(
        IOperationsQuickSignInRepository quick,
        IOperationsUserRepository users,
        IOperationsPasswordHasher hasher,
        IOperationsSecretGenerator secrets,
        OperationsSignInSteps steps,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        IOperationsDeviceClient devices,
        OperationsAccess access,
        OperationsDeviceBlocking blocking,
        IOperationsSettingsRepository settings)
    {
        _quick = quick ?? throw new ArgumentNullException(nameof(quick));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _devices = devices ?? throw new ArgumentNullException(nameof(devices));
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _blocking = blocking ?? throw new ArgumentNullException(nameof(blocking));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<BeginOperationsQuickSignInResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        BeginOperationsQuickSignInRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        // =====================================================
        // ONLY ON A LICENSED DEVICE OF THE COMPANY
        // =====================================================
        if (await PlaceAsync(context, request, cancellationToken) is not { DeviceId: { } deviceId } place)
        {
            return BeginOperationsQuickSignInResponse.Failure(OperationsAuthErrors.QuickDeviceRequired);
        }

        DateTimeOffset utcNow = _steps.UtcNow;
        bool byBadge = request.Badge is not null;
        OperationsStaffPin? pin = null;
        OperationsUser? user;

        // =====================================================
        // WHO: the badge in use, or the person and their PIN
        // =====================================================
        if (byBadge)
        {
            string? code = OperationsBadgeCode.Tidy(request.Badge);
            OperationsStaffBadge? badge = code is null ? null : await _quick.FindBadgeAsync(_secrets.HashToken(code), cancellationToken);

            if (badge is not { IsActive: true })
            {
                _audit.Record(null, "ops.auth.badge_refused", "OperationsDevice", deviceId, arguments: new { device = place.DeviceNumber, revoked = badge is not null });
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return BeginOperationsQuickSignInResponse.Failure(OperationsAuthErrors.QuickFailed);
            }

            user = await _users.GetAsync(badge.UserId, cancellationToken);
        }
        else
        {
            user = request.UserId is { } userId && userId != Guid.Empty ? await _users.GetAsync(userId, cancellationToken) : null;
            pin = user is null ? null : await _quick.FindPinAsync(user.Id, cancellationToken);

            if (user is null || pin is null)
            {
                return BeginOperationsQuickSignInResponse.Failure(OperationsAuthErrors.QuickFailed);
            }

            if (pin.IsLocked)
            {
                return BeginOperationsQuickSignInResponse.Failure(OperationsAuthErrors.PinLocked);
            }

            if (!OperationsStaffPin.IsWellFormed(request.Pin) || _hasher.Verify(request.Pin!, pin.PinHash) == OperationsPasswordCheck.Failed)
            {
                bool locked = pin.RecordFailure(utcNow);
                _audit.Record(null, locked ? "ops.auth.pin_locked" : "ops.auth.pin_failed", nameof(OperationsUser), user.Id, arguments: new { device = place.DeviceNumber });
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return BeginOperationsQuickSignInResponse.Failure(locked ? OperationsAuthErrors.PinLocked : OperationsAuthErrors.QuickFailed);
            }
        }

        if (user is null || !user.Account.IsSignInAllowed(utcNow))
        {
            if (user is not null)
            {
                _audit.Record(null, "ops.auth.sign_in_refused", nameof(OperationsUser), user.Id);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return BeginOperationsQuickSignInResponse.Failure(OperationsAuthErrors.QuickFailed);
        }

        if (user.PartnerWarehouseId is { } partnerWarehouseId && !(await _access.PartnerScopeAsync(context, partnerWarehouseId, cancellationToken)).Open)
        {
            return BeginOperationsQuickSignInResponse.Failure(OperationsAuthErrors.PartnerAgentsOff);
        }

        // Owner, 2026-10-29 (S-5): an outside account works only during its hours and until its end date.
        if (!await _access.MayWorkNowAsync(user, cancellationToken))
        {
            return BeginOperationsQuickSignInResponse.Failure(OperationsAuthErrors.OutsideHours);
        }

        // =====================================================
        // THE DEVICE RULES OF EVERY SIGN-IN (owner, R-6)
        // =====================================================
        if (await _blocking.IsBlockedAsync(deviceId, cancellationToken))
        {
            _audit.Record(null, "ops.auth.device_blocked_refused", nameof(OperationsUser), user.Id, arguments: new { device = place.DeviceNumber });
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return BeginOperationsQuickSignInResponse.Failure(OperationsAuthErrors.DeviceBlocked);
        }

        if (place.CountryChanged)
        {
            _audit.Record(user.Id, "ops.auth.country_changed", nameof(OperationsUser), user.Id,
                arguments: new { device = place.DeviceNumber, registered = place.RegisteredCountry, country = place.Country });
            await _blocking.BlockAsync(context.RequiredTenantId, deviceId, place.DeviceName, place.DeviceNumber, place.RegisteredCountry ?? string.Empty,
                place.Country ?? string.Empty, user.Id, request.PortalSlug, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return BeginOperationsQuickSignInResponse.Failure(OperationsAuthErrors.DeviceBlocked);
        }

        pin?.Succeeded(utcNow);

        // =====================================================
        // ONCE A DAY (owner H-8): the authenticator, or the password for a person without one
        // =====================================================
        string? timeZone = (await _settings.FindAsync(cancellationToken))?.TimeZone;
        OperationsDevicePerson? person = await _quick.FindDevicePersonAsync(deviceId, user.Id, cancellationToken);

        if (person?.FullSignInOn(OperationsTimeZones.Today(utcNow, timeZone), timeZone) == true)
        {
            OperationsIssuedSession? session = await _steps.StartSessionAsync(context, user, byBadge ? OperationsSignInSteps.BadgeMethod : OperationsSignInSteps.PinMethod,
                cancellationToken, place);
            return session is null
                ? BeginOperationsQuickSignInResponse.Failure(OperationsCommonErrors.ConcurrencyConflict)
                : BeginOperationsQuickSignInResponse.Success(session.Token, string.Empty, false);
        }

        OperationsSignInStage stage;

        if (user.HasSecondFactor)
        {
            stage = OperationsSignInStage.SecondFactor;
        }
        else if (!await _steps.NeedsSecondFactorAsync(user, cancellationToken) && user.Account.PasswordHash is not null)
        {
            stage = OperationsSignInStage.Password;
        }
        else
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return BeginOperationsQuickSignInResponse.Failure(OperationsAuthErrors.QuickFullSignInFirst);
        }

        string challenge = _steps.IssueChallenge(context, user, utcNow, place, stage);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? BeginOperationsQuickSignInResponse.Success(string.Empty, challenge, stage == OperationsSignInStage.Password)
            : BeginOperationsQuickSignInResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }

    // The device Platform verifies from the browser's cookie; none when there is no cookie, Platform refuses it or cannot answer.
    private async Task<OperationsSignInPlace?> PlaceAsync(OperationsAuthorizationContext context, BeginOperationsQuickSignInRequest request, CancellationToken cancellationToken)
    {
        if (request.DeviceId is not { } deviceId || deviceId == Guid.Empty || string.IsNullOrWhiteSpace(request.DeviceSecret))
        {
            return null;
        }

        try
        {
            OperationsDeviceCheck check = await _devices.VerifyAsync(context.RequiredTenantId, deviceId, request.DeviceSecret, false, null, cancellationToken);
            return check.Allowed ? new OperationsSignInPlace(deviceId, check.Name, check.Number, check.Country, _steps.Country) : null;
        }
        catch (Vixly.Operations.Application.Platform.OperationsPlatformUnavailableException)
        {
            return null;
        }
    }
}
