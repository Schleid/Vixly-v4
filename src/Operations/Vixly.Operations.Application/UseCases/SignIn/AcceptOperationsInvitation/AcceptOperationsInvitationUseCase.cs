using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.SignIn.AcceptOperationsInvitation;

// =========================================================
// FILE: AcceptOperationsInvitationUseCase.cs
//
// PURPOSE:
// Sets the password from an invitation link and continues to the second factor (Slice 1).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The link works once and for 72 hours
// - The Owner continues to authenticator app enrolment (D-O2)
// =========================================================
public sealed class AcceptOperationsInvitationUseCase
{
    private readonly IOperationsAccountTokenRepository _tokens;

    private readonly IOperationsUserRepository _users;

    private readonly IOperationsPasswordHasher _hasher;

    private readonly IOperationsSecretGenerator _secrets;

    private readonly OperationsSignInSteps _steps;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public AcceptOperationsInvitationUseCase(
        IOperationsAccountTokenRepository tokens,
        IOperationsUserRepository users,
        IOperationsPasswordHasher hasher,
        IOperationsSecretGenerator secrets,
        OperationsSignInSteps steps,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork)
    {
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<AcceptOperationsInvitationResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        AcceptOperationsInvitationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        OperationsAccountToken? token = string.IsNullOrWhiteSpace(request.Token)
            ? null
            : await _tokens.FindByTokenHashAsync(_secrets.HashToken(request.Token), cancellationToken);
        OperationsUser? user = token is null ? null : await _users.GetAsync(token.UserId, cancellationToken);

        if (token is null || user is null)
        {
            return AcceptOperationsInvitationResponse.Failure(OperationsAuthErrors.LinkInvalid);
        }

        Error? weak = OperationsPasswordPolicy.Check(request.Password, user.Email.Value);

        if (weak is not null)
        {
            return AcceptOperationsInvitationResponse.Failure(weak);
        }

        DateTimeOffset utcNow = _steps.UtcNow;

        try
        {
            token.Consume(OperationsAccountTokenPurpose.Invitation, utcNow);
            user.AcceptInvitation(_hasher.Hash(request.Password), utcNow);
        }
        catch (DomainException)
        {
            return AcceptOperationsInvitationResponse.Failure(OperationsAuthErrors.LinkInvalid);
        }

        _audit.Record(user.Id, "ops.auth.invitation_accepted", nameof(OperationsUser), user.Id);

        if (!await _steps.NeedsSecondFactorAsync(user, cancellationToken))
        {
            OperationsIssuedSession? session = await _steps.StartSessionAsync(context, user, "invitation", cancellationToken);
            return session is null
                ? AcceptOperationsInvitationResponse.Failure(OperationsCommonErrors.ConcurrencyConflict)
                : AcceptOperationsInvitationResponse.Success(string.Empty, false, session.Token);
        }

        string challengeToken = _steps.IssueChallenge(context, user, utcNow);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? AcceptOperationsInvitationResponse.Success(challengeToken, true, string.Empty)
            : AcceptOperationsInvitationResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
