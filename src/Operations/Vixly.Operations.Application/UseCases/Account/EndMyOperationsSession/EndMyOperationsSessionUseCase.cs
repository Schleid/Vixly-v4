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

namespace Vixly.Operations.Application.UseCases.Account.EndMyOperationsSession;

// =========================================================
// FILE: EndMyOperationsSessionUseCase.cs
//
// PURPOSE:
// Ends one of my sessions, for example on a lost phone (Slice 1D).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class EndMyOperationsSessionUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSessionRepository _sessions;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public EndMyOperationsSessionUseCase(
        OperationsAccess access,
        IOperationsSessionRepository sessions,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<EndMyOperationsSessionResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        EndMyOperationsSessionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _access.LoadAsync(context, cancellationToken) is not { } access)
        {
            return EndMyOperationsSessionResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        OperationsUserSession? session = await _sessions.GetAsync(request.SessionId, cancellationToken);

        if (session is null || session.UserId != access.User.Id)
        {
            return EndMyOperationsSessionResponse.Failure(OperationsCommonErrors.NotFound);
        }

        session.End(OperationsSessionEndReason.EndedByUser, _timeProvider.GetUtcNow());
        _audit.Record(access.User.Id, "ops.account.session_ended", "Session", session.Id);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? EndMyOperationsSessionResponse.Success()
            : EndMyOperationsSessionResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
