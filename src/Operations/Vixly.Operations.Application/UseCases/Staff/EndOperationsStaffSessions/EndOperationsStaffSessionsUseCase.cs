using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Staff;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.UseCases.Staff.EndOperationsStaffSessions;

// =========================================================
// FILE: EndOperationsStaffSessionsUseCase.cs
//
// PURPOSE:
// Signs a staff member out everywhere (Slice 1D).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class EndOperationsStaffSessionsUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsUserRepository _users;

    private readonly IOperationsSessionRepository _sessions;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public EndOperationsStaffSessionsUseCase(
        OperationsAccess access,
        IOperationsUserRepository users,
        IOperationsSessionRepository sessions,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<EndOperationsStaffSessionsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        EndOperationsStaffSessionsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.StaffEndSessions, true, cancellationToken);

        if (refused is not null)
        {
            return EndOperationsStaffSessionsResponse.Failure(refused);
        }

        OperationsUser? target = await _users.GetAsync(request.UserId, cancellationToken);

        if (target is null)
        {
            return EndOperationsStaffSessionsResponse.Failure(OperationsCommonErrors.NotFound);
        }

        // Nobody acts on their own account here; the Owner is protected; only the Owner acts on an Administrator.
        if (target.Id == access!.User.Id)
        {
            return EndOperationsStaffSessionsResponse.Failure(OperationsRoleErrors.NotYourself);
        }

        if (target.IsOwner)
        {
            return EndOperationsStaffSessionsResponse.Failure(OperationsRoleErrors.OwnerProtected);
        }

        if (target.IsAdministrator && !access.IsOwner)
        {
            return EndOperationsStaffSessionsResponse.Failure(OperationsAccessErrors.OwnerOnly);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        IReadOnlyList<OperationsUserSession> open = await _sessions.ListOpenAsync(target.Id, cancellationToken);

        foreach (OperationsUserSession session in open)
        {
            session.End(OperationsSessionEndReason.EndedByAdministrator, utcNow);
        }

        _audit.Record(access.User.Id, "ops.staff.sessions_ended", nameof(OperationsUser), target.Id, arguments: new { count = open.Count });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? EndOperationsStaffSessionsResponse.Success(open.Count)
            : EndOperationsStaffSessionsResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
