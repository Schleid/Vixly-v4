using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Legal;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.UseCases.Account.CompleteOperationsSecondFactorReset;

// =========================================================
// FILE: CompleteOperationsSecondFactorResetUseCase.cs
//
// PURPOSE:
// Completes the second-factor reset Vixly support sent to the Owner (D-O13, Slice 1C).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Owner only: Administrators and staff are reset inside the tenant (D-O14, 1D)
// - Order: password first (a wrong one spends nothing), then Platform spends the link, then the app and codes go and every session ends
// - The next step is the usual enrolment: new app, new recovery codes
// =========================================================
public sealed class CompleteOperationsSecondFactorResetUseCase
{
    private readonly IOperationsUserRepository _users;

    private readonly IOperationsPasswordHasher _hasher;

    private readonly IOperationsDeviceClient _devices;

    private readonly IOperationsSessionRepository _sessions;

    private readonly OperationsSignInSteps _steps;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public CompleteOperationsSecondFactorResetUseCase(
        IOperationsUserRepository users,
        IOperationsPasswordHasher hasher,
        IOperationsDeviceClient devices,
        IOperationsSessionRepository sessions,
        OperationsSignInSteps steps,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        _devices = devices ?? throw new ArgumentNullException(nameof(devices));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CompleteOperationsSecondFactorResetResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CompleteOperationsSecondFactorResetRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        // The link alone is not enough: the Owner proves the password too, so a stolen inbox cannot take the account.
        OperationsUser? user = EmailAddress.TryCreate(request.Email, out EmailAddress? email) ? await _users.FindByEmailAsync(email!, cancellationToken) : null;

        if (user is null || !user.IsOwner || !user.Account.IsActive || user.Account.PasswordHash is not { } hash
            || _hasher.Verify(request.Password ?? string.Empty, hash) == OperationsPasswordCheck.Failed)
        {
            if (user is { IsOwner: true })
            {
                user.RecordFailedSignIn(_timeProvider.GetUtcNow());
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return CompleteOperationsSecondFactorResetResponse.Failure(OperationsAuthErrors.SignInFailed);
        }

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return CompleteOperationsSecondFactorResetResponse.Failure(OperationsDeviceErrors.LinkInvalid);
        }

        OperationsPlatformCall<bool> call = await _devices.CompleteSecondFactorResetAsync(context.RequiredTenantId, request.Token.Trim(), cancellationToken);

        if (!call.Succeeded)
        {
            return CompleteOperationsSecondFactorResetResponse.Failure(call.Error!);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        user.ResetSecondFactor(utcNow);

        foreach (OperationsUserSession session in await _sessions.ListOpenAsync(user.Id, cancellationToken))
        {
            session.End(OperationsSessionEndReason.SecondFactorReset, utcNow);
        }

        string challenge = _steps.IssueChallenge(context, user, utcNow);
        _audit.Record(user.Id, "ops.auth.second_factor_reset", nameof(OperationsUser), user.Id);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? CompleteOperationsSecondFactorResetResponse.Success(challenge)
            : CompleteOperationsSecondFactorResetResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
