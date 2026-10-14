using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.UseCases.Users.EndPlatformUserSessions;

// =========================================================
// FILE: EndPlatformUserSessionsUseCase.cs
//
// PURPOSE:
// A security administrator signs a staff user out everywhere
// (ADR-0006 section 6).
//
// RESPONSIBILITIES:
// - Require platform.users.end_sessions and a recent second
//   factor
// - End every open session of the target and audit the count
//
// IMPORTANT NOTES:
// - Not on your own account: use Sign out everywhere instead
//   (design section 4, self-action rule)
// =========================================================
public sealed class EndPlatformUserSessionsUseCase
{
    private readonly PlatformUserAdministration _administration;

    private readonly IPlatformSessionRepository _sessions;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public EndPlatformUserSessionsUseCase(
        PlatformUserAdministration administration,
        IPlatformSessionRepository sessions,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<EndPlatformUserSessionsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        EndPlatformUserSessionsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformUserTarget target = await _administration.LoadTargetAsync(
            context,
            PlatformPermissions.UsersEndSessions,
            request.PlatformUserId,
            requireStepUp: true,
            cancellationToken);

        if (target.User is not PlatformUser user)
        {
            return EndPlatformUserSessionsResponse.Failure(target.Error!);
        }

        if (user.Id == context.PlatformUserId)
        {
            return EndPlatformUserSessionsResponse.Failure(PlatformIdentityErrors.CannotActOnOwnAccount);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        IReadOnlyList<PlatformUserSession> open = await _sessions.ListOpenAsync(user.Id, cancellationToken);

        foreach (PlatformUserSession session in open)
        {
            session.End(PlatformSessionEndReason.EndedByAdministrator, utcNow);
        }

        _audit.Record(
            context.PlatformUserId,
            "platform.users.sessions_ended",
            PlatformPermissions.UsersEndSessions,
            nameof(PlatformUser),
            user.Id,
            arguments: new { count = open.Count });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? EndPlatformUserSessionsResponse.Success(open.Count)
            : EndPlatformUserSessionsResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
