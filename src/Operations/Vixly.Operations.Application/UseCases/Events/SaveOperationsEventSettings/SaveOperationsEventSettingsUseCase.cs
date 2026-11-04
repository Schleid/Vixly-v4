using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Events;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Events.SaveOperationsEventSettings;

// =========================================================
// FILE: SaveOperationsEventSettingsUseCase.cs
//
// PURPOSE:
// Save Settings → Events (Slice 6C, E-3 and E-5): the emails and the standard causes switched off.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.settings.edit
// =========================================================
public sealed class SaveOperationsEventSettingsUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsEventSettingsUseCase(
        OperationsAccess access,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsEventSettingsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsEventSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsEdit, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsEventSettingsResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        if (settings is null)
        {
            settings = OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
            _settings.Add(settings);
        }

        object before = new { emails = settings.EventEmails, disabled = settings.DisabledEventCauses, pickup = settings.PickupEmails };

        try
        {
            settings.ChangeEvents(request.EventEmails, request.DisabledCauses, utcNow);
            settings.ChangePickupEmails(request.PickupEmails, utcNow);
        }
        catch (DomainException exception)
        {
            return SaveOperationsEventSettingsResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, "ops.events.settings_saved", nameof(OperationsTenantSettings), settings.Id, before: before, after: new { emails = settings.EventEmails, disabled = settings.DisabledEventCauses, pickup = settings.PickupEmails });
        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SaveOperationsEventSettingsResponse.Success()
            : SaveOperationsEventSettingsResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
