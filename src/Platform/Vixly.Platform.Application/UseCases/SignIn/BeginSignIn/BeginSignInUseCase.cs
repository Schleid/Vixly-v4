using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.UseCases.SignIn.BeginSignIn;

// =========================================================
// FILE: BeginSignInUseCase.cs
//
// PURPOSE:
// The password step of a staff sign-in.
//
// RESPONSIBILITIES:
// - Check email and password; count failures toward the
//   per-account lockout (5 failures, 15 minutes)
// - Upgrade a lower-cost BCrypt hash (migrated V3 users)
// - Issue a pending sign-in for the second factor, or for
//   enrolment when the user has none yet
// - Audit success and failure
//
// IMPORTANT NOTES:
// - A password alone never opens a session (ADR-0006)
// - Unknown email, wrong password, locked or inactive account
//   all answer SignInFailed, and an unknown email still runs a
//   full BCrypt check so the timing is the same
// =========================================================
public sealed class BeginSignInUseCase
{
    // A BCrypt hash (cost 12) of a random value nobody knows.
    private const string TimingEqualiser = "$2a$12$FC8a3NViTOEkLGndH5yps.gTnwWuns1YztQZwyA8N8qHH2rQTOMAm";

    private readonly IPlatformUserRepository _users;

    private readonly IPlatformPasswordHasher _hasher;

    private readonly PlatformSignInSteps _steps;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public BeginSignInUseCase(
        IPlatformUserRepository users,
        IPlatformPasswordHasher hasher,
        PlatformSignInSteps steps,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<BeginSignInResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        BeginSignInRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformUser? user = EmailAddress.TryCreate(request.Email, out EmailAddress? email)
            ? await _users.FindByEmailAsync(email!, cancellationToken)
            : null;

        DateTimeOffset utcNow = _steps.UtcNow;

        if (user is null || user.Account.PasswordHash is null)
        {
            _hasher.Verify(request.Password ?? string.Empty, TimingEqualiser);
            return BeginSignInResponse.Failure(PlatformAuthErrors.SignInFailed);
        }

        PlatformPasswordCheck check = _hasher.Verify(request.Password ?? string.Empty, user.Account.PasswordHash);

        if (!user.Account.IsSignInAllowed(utcNow))
        {
            _audit.Record(null, "platform.auth.sign_in_refused", null, nameof(PlatformUser), user.Id);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return BeginSignInResponse.Failure(PlatformAuthErrors.SignInFailed);
        }

        if (check == PlatformPasswordCheck.Failed)
        {
            user.RecordFailedSignIn(utcNow);
            _audit.Record(null, "platform.auth.password_failed", null, nameof(PlatformUser), user.Id);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return BeginSignInResponse.Failure(PlatformAuthErrors.SignInFailed);
        }

        if (check == PlatformPasswordCheck.SucceededNeedsRehash)
        {
            user.ChangePasswordHash(_hasher.Hash(request.Password!), utcNow);
        }

        string challengeToken = _steps.IssueChallenge(user, utcNow);

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return BeginSignInResponse.Failure(PlatformAuthErrors.SignInFailed);
        }

        return BeginSignInResponse.Success(
            challengeToken,
            !user.HasSecondFactor,
            user.Passkeys.Count > 0,
            user.AuthenticatorApp?.IsConfirmed == true,
            user.UnusedRecoveryCodeCount);
    }
}
