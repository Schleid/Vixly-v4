using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.UseCases.SignIn.BeginOperationsSignIn;

// =========================================================
// FILE: BeginOperationsSignInUseCase.cs
//
// PURPOSE:
// Checks the email and password of a staff member (Slice 1).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - A wrong email and a wrong password look the same and take the same time
// - Lockout after 5 failures (domain)
// - Owners always continue to a second factor or to enrolment
// =========================================================
public sealed class BeginOperationsSignInUseCase
{
    // A BCrypt hash (cost 12) of a random value nobody knows.
    private const string TimingEqualiser = "$2a$12$FC8a3NViTOEkLGndH5yps.gTnwWuns1YztQZwyA8N8qHH2rQTOMAm";

    private readonly IOperationsUserRepository _users;

    private readonly IOperationsPasswordHasher _hasher;

    private readonly OperationsSignInSteps _steps;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly IOperationsDeviceClient _devices;

    private readonly OperationsAccess _access;

    public BeginOperationsSignInUseCase(
        IOperationsUserRepository users,
        IOperationsPasswordHasher hasher,
        OperationsSignInSteps steps,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        IOperationsDeviceClient devices,
        OperationsAccess access)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _devices = devices ?? throw new ArgumentNullException(nameof(devices));
    }

    public async Task<BeginOperationsSignInResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        BeginOperationsSignInRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        // =====================================================
        // FIND THE USER (same work whether or not the email exists)
        // =====================================================
        OperationsUser? user = EmailAddress.TryCreate(request.Email, out EmailAddress? email)
            ? await _users.FindByEmailAsync(email!, cancellationToken)
            : null;
        DateTimeOffset utcNow = _steps.UtcNow;

        if (user is null || user.Account.PasswordHash is null)
        {
            _hasher.Verify(request.Password ?? string.Empty, TimingEqualiser);
            return BeginOperationsSignInResponse.Failure(OperationsAuthErrors.SignInFailed);
        }

        OperationsPasswordCheck check = _hasher.Verify(request.Password ?? string.Empty, user.Account.PasswordHash);

        if (!user.Account.IsSignInAllowed(utcNow))
        {
            _audit.Record(null, "ops.auth.sign_in_refused", nameof(OperationsUser), user.Id);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return BeginOperationsSignInResponse.Failure(OperationsAuthErrors.SignInFailed);
        }

        if (check == OperationsPasswordCheck.Failed)
        {
            user.RecordFailedSignIn(utcNow);
            _audit.Record(null, "ops.auth.password_failed", nameof(OperationsUser), user.Id);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return BeginOperationsSignInResponse.Failure(OperationsAuthErrors.SignInFailed);
        }

        if (check == OperationsPasswordCheck.SucceededNeedsRehash)
        {
            user.ChangePasswordHash(_hasher.Hash(request.Password!), utcNow);
        }

        // A partner agent (Slice 7 step 4, R-8) signs in only while the plan includes partner agents and its warehouse is still the partner's (fails closed).
        if (user.PartnerWarehouseId is { } partnerWarehouseId && !(await _access.PartnerScopeAsync(context, partnerWarehouseId, cancellationToken)).Open)
        {
            _audit.Record(null, "ops.auth.partner_agents_off", nameof(OperationsUser), user.Id);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return BeginOperationsSignInResponse.Failure(OperationsAuthErrors.PartnerAgentsOff);
        }

        // =====================================================
        // WHERE (owner, 2026-10-21): the licensed device, and a country other than the one it was registered in
        // =====================================================
        OperationsSignInPlace place = await PlaceAsync(context, request, cancellationToken);

        if (place.CountryChanged)
        {
            _audit.Record(user.Id, "ops.auth.country_changed", nameof(OperationsUser), user.Id,
                arguments: new { device = place.DeviceNumber, registered = place.RegisteredCountry, country = place.Country });

            // A second factor confirms it; without one set up there is nothing to confirm with (setting one up here would let the password alone through).
            if (!user.HasSecondFactor)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return BeginOperationsSignInResponse.Failure(OperationsAuthErrors.CountryNeedsSecondFactor);
            }
        }

        // =====================================================
        // PASSWORD ALONE: only for staff who need no second factor and have none (D-O2)
        // =====================================================
        if (!place.CountryChanged && !user.HasSecondFactor && !await _steps.NeedsSecondFactorAsync(user, cancellationToken))
        {
            OperationsIssuedSession? session = await _steps.StartSessionAsync(context, user, "password", cancellationToken, place);
            return session is null
                ? BeginOperationsSignInResponse.Failure(OperationsCommonErrors.ConcurrencyConflict)
                : BeginOperationsSignInResponse.Success(string.Empty, false, session.Token);
        }

        string challengeToken = _steps.IssueChallenge(context, user, utcNow, place);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? BeginOperationsSignInResponse.Success(challengeToken, !user.HasSecondFactor, string.Empty)
            : BeginOperationsSignInResponse.Failure(OperationsAuthErrors.SignInFailed);
    }

    // The device Platform verifies from the browser's cookie; none when there is no cookie or Platform refuses it (the device gate deals with that).
    private async Task<OperationsSignInPlace> PlaceAsync(OperationsAuthorizationContext context, BeginOperationsSignInRequest request,
        CancellationToken cancellationToken)
    {
        if (request.DeviceId is not { } deviceId || deviceId == Guid.Empty || string.IsNullOrWhiteSpace(request.DeviceSecret))
        {
            return OperationsSignInPlace.Nowhere(_steps.Country);
        }

        OperationsDeviceCheck check;

        // Never rotated here: a lost or doubled answer would leave the browser an old secret and the device revoked later (review).
        try
        {
            // No country sent: a device takes its first country from a fully signed-in session only (the gate), never before the second factor.
            check = await _devices.VerifyAsync(context.RequiredTenantId, deviceId, request.DeviceSecret, false, null, cancellationToken);
        }
        catch (OperationsPlatformUnavailableException)
        {
            // The device gate checks again on the first staff page, with the country (it shows the unavailable page meanwhile).
            return OperationsSignInPlace.Nowhere(_steps.Country);
        }

        return check.Allowed
            ? new OperationsSignInPlace(deviceId, check.Name, check.Number, check.Country, _steps.Country)
            : OperationsSignInPlace.Nowhere(_steps.Country);
    }
}
