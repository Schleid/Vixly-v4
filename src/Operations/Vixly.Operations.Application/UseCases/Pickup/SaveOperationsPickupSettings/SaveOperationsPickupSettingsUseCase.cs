using Vixly.Operations.Application.Arrivals;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Pickup;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Pickup;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Pickup.SaveOperationsPickupSettings;

// =========================================================
// FILE: SaveOperationsPickupSettingsUseCase.cs
//
// PURPOSE:
// Save Settings → Pickup (Slice 7 step 2, R-4, R-5, R-10).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.settings.edit
// =========================================================
public sealed class SaveOperationsPickupSettingsUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsPickupSettingsUseCase(
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

    public async Task<SaveOperationsPickupSettingsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsPickupSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsEdit, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsPickupSettingsResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        if (settings is null)
        {
            settings = OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
            _settings.Add(settings);
        }

        object before = new { proofs = settings.PickupProofs.ToString(), charges = settings.DestinationCharges, storage = settings.DestinationStorage, settings.DestinationStorageFreeDays,
            settings.DestinationStorageFeePerDay };

        try
        {
            settings.ChangePickup(request.Proofs, request.DestinationCharges, request.DestinationStorage, request.FreeDays, request.FeePerDay, utcNow);
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            return SaveOperationsPickupSettingsResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, "ops.pickup.settings_saved", nameof(OperationsTenantSettings), settings.Id, before: before, after: new { proofs = settings.PickupProofs.ToString(),
            charges = settings.DestinationCharges, storage = settings.DestinationStorage, settings.DestinationStorageFreeDays, settings.DestinationStorageFeePerDay });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return SaveOperationsPickupSettingsResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return SaveOperationsPickupSettingsResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
