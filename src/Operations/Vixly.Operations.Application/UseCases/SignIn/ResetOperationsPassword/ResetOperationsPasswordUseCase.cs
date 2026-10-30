using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.UseCases.SignIn.ResetOperationsPassword;

// =========================================================
// FILE: ResetOperationsPasswordUseCase.cs
//
// PURPOSE:
// Sets a new password from an emailed reset link (Slice 1C).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The second factor is unchanged and still asked at the next sign-in
// - A lost second factor is a Vixly support case for the Owner (D-O13)
// =========================================================
public sealed class ResetOperationsPasswordUseCase
{
    private readonly IOperationsAccountTokenRepository _tokens;

    private readonly IOperationsUserRepository _users;

    private readonly IOperationsPasswordHasher _hasher;

    private readonly IOperationsSecretGenerator _secrets;

    private readonly IOperationsSessionRepository _sessions;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ResetOperationsPasswordUseCase(
        IOperationsAccountTokenRepository tokens,
        IOperationsUserRepository users,
        IOperationsPasswordHasher hasher,
        IOperationsSecretGenerator secrets,
        IOperationsSessionRepository sessions,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ResetOperationsPasswordResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ResetOperationsPasswordRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        OperationsAccountToken? token = string.IsNullOrWhiteSpace(request.Token)
            ? null
            : await _tokens.FindByTokenHashAsync(_secrets.HashToken(request.Token.Trim()), cancellationToken);
        OperationsUser? user = token is null ? null : await _users.GetAsync(token.UserId, cancellationToken);

        if (token is null || user is null || token.Purpose != OperationsAccountTokenPurpose.PasswordReset)
        {
            return ResetOperationsPasswordResponse.Failure(OperationsAuthErrors.LinkInvalid);
        }

        if (OperationsPasswordPolicy.Check(request.Password, user.Email.Value) is { } weak)
        {
            return ResetOperationsPasswordResponse.Failure(weak);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            token.Consume(OperationsAccountTokenPurpose.PasswordReset, utcNow);
            user.ChangePasswordHash(_hasher.Hash(request.Password), utcNow);
        }
        catch (DomainException)
        {
            return ResetOperationsPasswordResponse.Failure(OperationsAuthErrors.LinkInvalid);
        }

        // Every session ends: whoever knew the old password is signed out everywhere.
        foreach (OperationsUserSession session in await _sessions.ListOpenAsync(user.Id, cancellationToken))
        {
            session.End(OperationsSessionEndReason.PasswordChanged, utcNow);
        }

        _audit.Record(user.Id, "ops.auth.password_reset", nameof(OperationsUser), user.Id);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ResetOperationsPasswordResponse.Success()
            : ResetOperationsPasswordResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
