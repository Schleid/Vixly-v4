using Vixly.Operations.Application.Arrivals;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Delivery;
using Vixly.Operations.Application.Events;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Pickup;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Delivery;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Pickup;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Delivery.SaveOperationsDeliverySettings;

// =========================================================
// FILE: SaveOperationsDeliverySettingsUseCase.cs
//
// PURPOSE:
// Save Settings → Delivery (Slice 7 step 3, R-12 to R-15).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.settings.edit
// =========================================================
public sealed class SaveOperationsDeliverySettingsUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsDeliverySettingsUseCase(
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

    public async Task<SaveOperationsDeliverySettingsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsDeliverySettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsEdit, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsDeliverySettingsResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        if (settings is null)
        {
            settings = OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
            _settings.Add(settings);
        }

        object before = new { recording = settings.DeliveryRecording.ToString(), cash = settings.CashOnDelivery, tries = settings.DeliveryTries, after = settings.AfterFailedDelivery.ToString(),
            proofs = settings.DeliveryProofs.ToString(), emails = settings.DeliveryEmails };

        try
        {
            settings.ChangeDelivery(request.Recording, request.CashOnDelivery, request.Tries, request.AfterFailed, request.Proofs, request.Emails, utcNow);
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            return SaveOperationsDeliverySettingsResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, "ops.delivery.settings_saved", nameof(OperationsTenantSettings), settings.Id, before: before, after: new { recording = settings.DeliveryRecording.ToString(),
            cash = settings.CashOnDelivery, tries = settings.DeliveryTries, after = settings.AfterFailedDelivery.ToString(), proofs = settings.DeliveryProofs.ToString(),
            emails = settings.DeliveryEmails });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return SaveOperationsDeliverySettingsResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return SaveOperationsDeliverySettingsResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
