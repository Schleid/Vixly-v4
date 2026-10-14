using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.UseCases.MyAccount.EndMySessions;

// =========================================================
// FILE: EndMySessionsUseCase.cs
//
// PURPOSE:
// The signed-in user ends one of their other sessions, or all of
// them (ADR-0006 section 6).
//
// RESPONSIBILITIES:
// - End the named session when it is theirs and not this one; or
//   every other open session
// - Audit the count and commit
//
// IMPORTANT NOTES:
// - No step-up: ending sessions only takes access away
// - A session of another user is answered NotFound, like one that
//   does not exist
// =========================================================
public sealed class EndMySessionsUseCase
{
    private readonly IPlatformSessionRepository _sessions;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public EndMySessionsUseCase(
        IPlatformSessionRepository sessions,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<EndMySessionsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        EndMySessionsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (request.SessionId is Guid named && named == context.SessionId)
        {
            return EndMySessionsResponse.Failure(PlatformAuthErrors.CurrentSessionNotEnded);
        }

        List<PlatformUserSession> targets = (await _sessions.ListOpenAsync(context.PlatformUserId, cancellationToken))
            .Where(session => session.Id != context.SessionId && (request.SessionId is null || session.Id == request.SessionId))
            .ToList();

        if (request.SessionId is not null && targets.Count == 0)
        {
            return EndMySessionsResponse.Failure(PlatformCommonErrors.NotFound);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        foreach (PlatformUserSession session in targets)
        {
            session.End(PlatformSessionEndReason.EndedByUser, utcNow);
        }

        _audit.Record(context.PlatformUserId, "platform.account.sessions_ended", null, nameof(PlatformUser), context.PlatformUserId, arguments: new { count = targets.Count });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? EndMySessionsResponse.Success(targets.Count)
            : EndMySessionsResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
