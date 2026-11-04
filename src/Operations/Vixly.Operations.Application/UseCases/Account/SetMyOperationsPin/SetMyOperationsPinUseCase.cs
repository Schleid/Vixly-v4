using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Staff;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Application.UseCases.Account.SetMyOperationsPin;

// =========================================================
// FILE: SetMyOperationsPinUseCase.cs
//
// PURPOSE:
// Sets or changes the signed-in person's 4-digit PIN for licensed devices (Slice 12A, owner H-8).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Only from a full sign-in, with the current password
// - A new PIN clears the wrong-PIN count
// =========================================================
public sealed class SetMyOperationsPinUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsQuickSignInRepository _quick;

    private readonly IOperationsSessionRepository _sessions;

    private readonly IOperationsPasswordHasher _hasher;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SetMyOperationsPinUseCase(
        OperationsAccess access,
        IOperationsQuickSignInRepository quick,
        IOperationsSessionRepository sessions,
        IOperationsPasswordHasher hasher,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _quick = quick ?? throw new ArgumentNullException(nameof(quick));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SetMyOperationsPinResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SetMyOperationsPinRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _access.LoadAsync(context, cancellationToken) is not { } access)
        {
            return SetMyOperationsPinResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        OperationsUser user = access.User;

        if (context.SessionId is not { } sessionId || await _sessions.GetAsync(sessionId, cancellationToken) is not { IsQuick: false })
        {
            return SetMyOperationsPinResponse.Failure(OperationsStaffErrors.PinNeedsFullSignIn);
        }

        if (user.Account.PasswordHash is not { } hash || _hasher.Verify(request.CurrentPassword ?? string.Empty, hash) == OperationsPasswordCheck.Failed)
        {
            return SetMyOperationsPinResponse.Failure(OperationsStaffErrors.PasswordWrong);
        }

        if (!OperationsQuickSignIn.IsAcceptablePin(request.Pin))
        {
            return SetMyOperationsPinResponse.Failure(OperationsStaffErrors.PinInvalid);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        string pinHash = _hasher.Hash(request.Pin!);

        if (await _quick.FindPinAsync(user.Id, cancellationToken) is { } pin)
        {
            pin.Change(pinHash, utcNow);
        }
        else
        {
            _quick.Add(OperationsStaffPin.Set(context.RequiredTenantId, user.Id, pinHash, utcNow));
        }

        _audit.Record(user.Id, "ops.account.pin_set", nameof(OperationsUser), user.Id);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SetMyOperationsPinResponse.Success()
            : SetMyOperationsPinResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
