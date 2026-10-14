using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.UseCases.SignIn.SignOut;

// =========================================================
// FILE: SignOutUseCase.cs
//
// PURPOSE:
// A staff member signs out.
//
// RESPONSIBILITIES:
// - End the current session, and optionally all others
//
// IMPORTANT NOTES:
// - Needs no permission: every signed-in user may sign out
// =========================================================
public sealed class SignOutUseCase
{
    private readonly IPlatformSessionRepository _sessions;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SignOutUseCase(
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

    public async Task<SignOutResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        SignOutRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        foreach (PlatformUserSession session in await _sessions.ListOpenAsync(context.PlatformUserId, cancellationToken))
        {
            if (session.Id == context.SessionId)
            {
                session.End(PlatformSessionEndReason.SignedOut, utcNow);
            }
            else if (request.EverywhereElseToo)
            {
                session.End(PlatformSessionEndReason.EndedByUser, utcNow);
            }
        }

        _audit.Record(
            context.PlatformUserId,
            "platform.auth.signed_out",
            null,
            nameof(PlatformUser),
            context.PlatformUserId,
            arguments: new { everywhere = request.EverywhereElseToo });
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return SignOutResponse.Success();
    }
}
