using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Application.UseCases.SignIn.SignOutOperations;

// =========================================================
// FILE: SignOutOperationsUseCase.cs
//
// PURPOSE:
// Ends the current staff session, and optionally the others (Slice 1).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Signing out twice is harmless
// =========================================================
public sealed class SignOutOperationsUseCase
{
    private readonly IOperationsSessionRepository _sessions;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SignOutOperationsUseCase(
        IOperationsSessionRepository sessions,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SignOutOperationsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SignOutOperationsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (context.UserId is not { } userId)
        {
            return SignOutOperationsResponse.Success();
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        foreach (OperationsUserSession session in await _sessions.ListOpenAsync(userId, cancellationToken))
        {
            if (session.Id == context.SessionId)
            {
                session.End(OperationsSessionEndReason.SignedOut, utcNow);
            }
            else if (request.EverywhereElseToo)
            {
                session.End(OperationsSessionEndReason.EndedByUser, utcNow);
            }
        }

        _audit.Record(userId, "ops.auth.signed_out", nameof(OperationsUser), userId, arguments: new { everywhere = request.EverywhereElseToo });
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return SignOutOperationsResponse.Success();
    }
}
