using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.ScanIntoOperationsRepackBox;

// =========================================================
// FILE: ScanIntoOperationsRepackBoxUseCase.cs
//
// PURPOSE:
// A worker scans a parcel of the shipment into its open repack box (Slice 6A, D-4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bundles.fill, at the box's warehouse
// - Only the box's own shipment's parcels; the same scan twice is a repeat
// =========================================================
public sealed class ScanIntoOperationsRepackBoxUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsBundleRepository _bundles;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly OperationsBundling _bundling;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ScanIntoOperationsRepackBoxUseCase(
        OperationsBundleDesk desk,
        IOperationsBundleRepository bundles,
        IOperationsReceivingRepository receiving,
        IOperationsTrackingRepository tracking,
        OperationsBundling bundling,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _bundling = bundling ?? throw new ArgumentNullException(nameof(bundling));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ScanIntoOperationsRepackBoxResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ScanIntoOperationsRepackBoxRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.BundlesFill }, cancellationToken);

        if (refused is not null)
        {
            return ScanIntoOperationsRepackBoxResponse.Failure(refused);
        }

        if (await _bundles.GetBoxAsync(request.BoxId, cancellationToken) is not { } box || warehouses.All(item => item.Id != box.WarehouseId))
        {
            return ScanIntoOperationsRepackBoxResponse.Failure(OperationsBundleErrors.NotFound);
        }

        string code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();

        if (code.Length is 0 or > 40 || await _receiving.FindParcelByNumberAsync(code, cancellationToken) is not { } found
            || await _tracking.GetParcelAsync(found.Id, cancellationToken) is not { } parcel)
        {
            return ScanIntoOperationsRepackBoxResponse.Failure(OperationsBundleErrors.UnknownCode);
        }

        if (parcel.BoxId == box.Id)
        {
            return ScanIntoOperationsRepackBoxResponse.Success(parcel.Id, true);
        }

        if (box.Status != OperationsRepackBoxStatus.Open)
        {
            return ScanIntoOperationsRepackBoxResponse.Failure(OperationsBundleErrors.BoxNotOpen);
        }

        if (parcel.ShipmentId != box.ShipmentId)
        {
            return ScanIntoOperationsRepackBoxResponse.Failure(OperationsBundleErrors.WrongShipment);
        }

        if (parcel.BoxId is not null || parcel.BundleId is not null)
        {
            return ScanIntoOperationsRepackBoxResponse.Failure(parcel.BoxId is not null ? OperationsBundleErrors.InAnotherBox : OperationsBundleErrors.InAnotherBundle);
        }

        if (OperationsBundling.Ready(parcel, await _receiving.GetShipmentAsync(box.ShipmentId, cancellationToken)) is { } notReady)
        {
            return ScanIntoOperationsRepackBoxResponse.Failure(notReady);
        }

        // On a bundle's list (review): it goes into that bundle loose, or off the list first.
        if ((await _bundles.ListOpenItemsAsync(new[] { parcel.Id }, Array.Empty<Guid>(), cancellationToken)).Count > 0)
        {
            return ScanIntoOperationsRepackBoxResponse.Failure(OperationsBundleErrors.OnAnotherList);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            box.Take(parcel.WeightKg ?? 0m, utcNow);
            parcel.Box(box.Id, utcNow);
        }
        catch (DomainException exception)
        {
            return ScanIntoOperationsRepackBoxResponse.Failure(exception.Error);
        }

        await _bundling.BoxedAsync(parcel, box, access!.User.Id, (request.ScanKey == Guid.Empty ? Guid.NewGuid() : request.ScanKey).ToString("N"), cancellationToken);

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ScanIntoOperationsRepackBoxResponse.Success(parcel.Id, false);
        }

        _unitOfWork.DiscardChanges();
        return ScanIntoOperationsRepackBoxResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
