using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Identity;
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

    public BeginOperationsSignInUseCase(
        IOperationsUserRepository users,
        IOperationsPasswordHasher hasher,
        OperationsSignInSteps steps,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
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

        // =====================================================
        // PASSWORD ALONE: only for staff who need no second factor and have none (D-O2)
        // =====================================================
        if (!user.HasSecondFactor && !await _steps.NeedsSecondFactorAsync(user, cancellationToken))
        {
            OperationsIssuedSession? session = await _steps.StartSessionAsync(context, user, "password", cancellationToken);
            return session is null
                ? BeginOperationsSignInResponse.Failure(OperationsCommonErrors.ConcurrencyConflict)
                : BeginOperationsSignInResponse.Success(string.Empty, false, session.Token);
        }

        string challengeToken = _steps.IssueChallenge(context, user, utcNow);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? BeginOperationsSignInResponse.Success(challengeToken, !user.HasSecondFactor, string.Empty)
            : BeginOperationsSignInResponse.Failure(OperationsAuthErrors.SignInFailed);
    }
}
