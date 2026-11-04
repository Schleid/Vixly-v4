using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Staff;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Account.ChangeMyOperationsPassword;

// =========================================================
// FILE: ChangeMyOperationsPasswordUseCase.cs
//
// PURPOSE:
// Changes my password; every other session of mine ends (Slice 1D).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ChangeMyOperationsPasswordUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsPasswordHasher _hasher;

    private readonly IOperationsSessionRepository _sessions;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ChangeMyOperationsPasswordUseCase(
        OperationsAccess access,
        IOperationsPasswordHasher hasher,
        IOperationsSessionRepository sessions,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ChangeMyOperationsPasswordResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChangeMyOperationsPasswordRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _access.LoadAsync(context, cancellationToken) is not { } access)
        {
            return ChangeMyOperationsPasswordResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        OperationsUser user = access.User;

        if (user.Account.PasswordHash is not { } hash || _hasher.Verify(request.CurrentPassword ?? string.Empty, hash) == OperationsPasswordCheck.Failed)
        {
            return ChangeMyOperationsPasswordResponse.Failure(OperationsStaffErrors.PasswordWrong);
        }

        if (OperationsPasswordPolicy.Check(request.NewPassword, user.Email.Value) is { } weak)
        {
            return ChangeMyOperationsPasswordResponse.Failure(weak);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        user.ChangePasswordHash(_hasher.Hash(request.NewPassword), utcNow);

        foreach (OperationsUserSession session in await _sessions.ListOpenAsync(user.Id, cancellationToken))
        {
            if (session.Id != context.SessionId)
            {
                session.End(OperationsSessionEndReason.PasswordChanged, utcNow);
            }
        }

        _audit.Record(user.Id, "ops.account.password_changed", nameof(OperationsUser), user.Id);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ChangeMyOperationsPasswordResponse.Success()
            : ChangeMyOperationsPasswordResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
