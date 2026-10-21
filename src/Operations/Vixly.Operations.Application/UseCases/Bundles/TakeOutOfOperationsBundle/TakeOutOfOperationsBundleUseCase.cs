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

namespace Vixly.Operations.Application.UseCases.Bundles.TakeOutOfOperationsBundle;

// =========================================================
// FILE: TakeOutOfOperationsBundleUseCase.cs
//
// PURPOSE:
// Take a parcel or a repack box out of an open bundle: a wrong scan (Slice 6A, D-6).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bundles.fill, at the bundle's warehouse; only from an open bundle
// - Its packed event is voided, so its milestone goes back to final (ADR-0021)
// =========================================================
public sealed class TakeOutOfOperationsBundleUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsBundleRepository _bundles;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly OperationsBundling _bundling;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public TakeOutOfOperationsBundleUseCase(
        OperationsBundleDesk desk,
        IOperationsBundleRepository bundles,
        IOperationsTrackingRepository tracking,
        OperationsBundling bundling,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _bundling = bundling ?? throw new ArgumentNullException(nameof(bundling));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<TakeOutOfOperationsBundleResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        TakeOutOfOperationsBundleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.BundlesFill }, cancellationToken);

        if (refused is not null)
        {
            return TakeOutOfOperationsBundleResponse.Failure(refused);
        }

        if (await _bundles.GetBundleAsync(request.BundleId, cancellationToken) is not { } bundle || warehouses.All(item => item.Id != bundle.WarehouseId))
        {
            return TakeOutOfOperationsBundleResponse.Failure(OperationsBundleErrors.NotFound);
        }

        List<OperationsParcel> parcels = (await _bundles.ListBundleParcelsForUpdateAsync(bundle.Id, cancellationToken)).ToList();

        // A parcel that went in with its box comes out with it.
        Guid? boxId = request.BoxId ?? parcels.FirstOrDefault(item => item.Id == request.ParcelId)?.BoxId;
        OperationsRepackBox? box = boxId is { } id ? (await _bundles.ListBundleBoxesForUpdateAsync(bundle.Id, cancellationToken)).FirstOrDefault(item => item.Id == id) : null;
        List<OperationsParcel> leaving = box is not null ? parcels.Where(item => item.BoxId == box.Id).ToList() : parcels.Where(item => item.Id == request.ParcelId).ToList();

        if (leaving.Count == 0 || (boxId is not null && box is null))
        {
            return TakeOutOfOperationsBundleResponse.Failure(OperationsBundleErrors.NotInBundle);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            if (box is not null)
            {
                bundle.Release(leaving.Count, box.WeightKg ?? 0m, bundle.VolumetricOf(box.LengthCm ?? 0m, box.WidthCm ?? 0m, box.HeightCm ?? 0m), utcNow);
                box.TakeOut(utcNow);
            }
            else
            {
                OperationsParcel parcel = leaving[0];
                bundle.Release(1, parcel.WeightKg ?? 0m, bundle.VolumetricOf(parcel.LengthCm ?? 0m, parcel.WidthCm ?? 0m, parcel.HeightCm ?? 0m), utcNow);
            }
        }
        catch (DomainException exception)
        {
            return TakeOutOfOperationsBundleResponse.Failure(exception.Error);
        }

        foreach (OperationsParcel parcel in leaving)
        {
            parcel.Unpack(utcNow);
            await _bundling.VoidLatestAsync(parcel, OperationsTrackingCodes.Packed, "taken_out", access!.User.Id, cancellationToken);
        }

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return TakeOutOfOperationsBundleResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return TakeOutOfOperationsBundleResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
