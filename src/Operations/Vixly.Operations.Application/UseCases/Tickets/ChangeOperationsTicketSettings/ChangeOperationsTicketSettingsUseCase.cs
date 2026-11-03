using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tickets;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Tickets;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.UseCases.Tickets.ChangeOperationsTicketSettings;

// =========================================================
// FILE: ChangeOperationsTicketSettingsUseCase.cs
//
// PURPOSE:
// How long a ticket may wait before the managers are told: not taken after so many hours, not resolved after so many days (owner, 2026-10-29, ticket desk section 5).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.settings.edit with the second check
// =========================================================
public sealed class ChangeOperationsTicketSettingsUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ChangeOperationsTicketSettingsUseCase(OperationsAccess access, IOperationsSettingsRepository settings, OperationsAuditTrail audit, IOperationsUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ChangeOperationsTicketSettingsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChangeOperationsTicketSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsEdit, true, cancellationToken);

        if (refused is not null)
        {
            return ChangeOperationsTicketSettingsResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        if (settings is null)
        {
            settings = OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
            _settings.Add(settings);
        }

        (int hours, int days) = (settings.TicketTakeHours, settings.TicketResolveDays);

        try
        {
            settings.ChangeTickets(request.TakeHours, request.ResolveDays, utcNow);
        }
        catch (DomainException exception)
        {
            return ChangeOperationsTicketSettingsResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, "ops.settings.tickets_changed", nameof(OperationsTenantSettings), settings.Id, before: new { hours, days },
            after: new { hours = request.TakeHours, days = request.ResolveDays });
        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ChangeOperationsTicketSettingsResponse.Success()
            : ChangeOperationsTicketSettingsResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
