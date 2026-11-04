using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.CloseOperationsRepackBox;

// =========================================================
// FILE: CloseOperationsRepackBoxUseCase.cs
//
// PURPOSE:
// Close a repack box with its weight and size once every parcel is in (Slice 6A, D-4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bundles.manage or ops.bundles.fill, at its warehouse
// - Every parcel of the shipment must be in it: they are repacked together
// =========================================================
public sealed class CloseOperationsRepackBoxUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsBundleRepository _bundles;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public CloseOperationsRepackBoxUseCase(
        OperationsBundleDesk desk,
        IOperationsBundleRepository bundles,
        IOperationsReceivingRepository receiving,
        IOperationsSettingsRepository settings,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CloseOperationsRepackBoxResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CloseOperationsRepackBoxRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.BundlesManage, OperationsPermissions.BundlesFill }, cancellationToken);

        if (refused is not null)
        {
            return CloseOperationsRepackBoxResponse.Failure(refused);
        }

        if (await _bundles.GetBoxAsync(request.BoxId, cancellationToken) is not { } box || warehouses.All(item => item.Id != box.WarehouseId))
        {
            return CloseOperationsRepackBoxResponse.Failure(OperationsBundleErrors.NotFound);
        }

        if (box.Status == OperationsRepackBoxStatus.Closed)
        {
            return CloseOperationsRepackBoxResponse.Success();
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        int count = OperationsShipmentBooking.Counted(await _receiving.ListShipmentParcelsAsync(box.ShipmentId, cancellationToken)).Count;

        try
        {
            OperationsMeasurements measurements = OperationsMeasurements.Create(OperationsUnits.ToKilograms(request.Weight, settings.WeightUnit), OperationsUnits.ToCentimetres(request.Length, settings.DimensionUnit),
                OperationsUnits.ToCentimetres(request.Width, settings.DimensionUnit), OperationsUnits.ToCentimetres(request.Height, settings.DimensionUnit));
            box.Close(measurements, count, access!.User.Id, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return CloseOperationsRepackBoxResponse.Failure(exception.Error);
        }

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return CloseOperationsRepackBoxResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return CloseOperationsRepackBoxResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
