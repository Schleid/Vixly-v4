using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Staff.SetOperationsStaffOutside;

// =========================================================
// FILE: SetOperationsStaffOutsideUseCase.cs
//
// PURPOSE:
// Makes a staff account an outside one (a call centre's agent), with its end date and working hours, or an ordinary one again (owner, 2026-10-29, S-5).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.staff.edit for the whole company, with step-up; never on oneself; never the Owner, an Administrator or a partner's agent
// =========================================================
public sealed class SetOperationsStaffOutsideUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsUserRepository _users;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SetOperationsStaffOutsideUseCase(OperationsAccess access, IOperationsUserRepository users, IOperationsSettingsRepository settings, OperationsAuditTrail audit, IOperationsUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SetOperationsStaffOutsideResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SetOperationsStaffOutsideRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.StaffEdit, true, cancellationToken);

        if (refused is not null)
        {
            return SetOperationsStaffOutsideResponse.Failure(refused);
        }

        if (await _users.GetAsync(request.UserId, cancellationToken) is not { } target)
        {
            return SetOperationsStaffOutsideResponse.Failure(OperationsCommonErrors.NotFound);
        }

        if (target.Id == access!.User.Id)
        {
            return SetOperationsStaffOutsideResponse.Failure(OperationsRoleErrors.NotYourself);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            if (request.Outside)
            {
                DateOnly today = OperationsTimeZones.Today(utcNow, (await _settings.FindAsync(cancellationToken))?.TimeZone);
                target.MakeOutside(OperationsOutsideHours.Create(request.EndsOn, request.Days, request.FromMinute, request.ToMinute), today, utcNow);
            }
            else
            {
                target.MakeInside(utcNow);
            }
        }
        catch (DomainException exception)
        {
            return SetOperationsStaffOutsideResponse.Failure(exception.Error);
        }

        _audit.Record(access.User.Id, "ops.staff.outside_changed", nameof(OperationsUser), target.Id,
            arguments: new { outside = request.Outside, endsOn = request.EndsOn, days = request.Days, from = request.FromMinute, to = request.ToMinute });
        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SetOperationsStaffOutsideResponse.Success()
            : SetOperationsStaffOutsideResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
