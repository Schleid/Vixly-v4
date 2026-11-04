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

namespace Vixly.Operations.Application.UseCases.Bundles.TakeOutOfOperationsRepackBox;

// =========================================================
// FILE: TakeOutOfOperationsRepackBoxUseCase.cs
//
// PURPOSE:
// Take a parcel out of an open repack box (Slice 6A, D-4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bundles.fill, at the box's warehouse; only from an open box
// =========================================================
public sealed class TakeOutOfOperationsRepackBoxUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsBundleRepository _bundles;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly OperationsBundling _bundling;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public TakeOutOfOperationsRepackBoxUseCase(
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

    public async Task<TakeOutOfOperationsRepackBoxResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        TakeOutOfOperationsRepackBoxRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.BundlesFill }, cancellationToken);

        if (refused is not null)
        {
            return TakeOutOfOperationsRepackBoxResponse.Failure(refused);
        }

        if (await _bundles.GetBoxAsync(request.BoxId, cancellationToken) is not { } box || warehouses.All(item => item.Id != box.WarehouseId))
        {
            return TakeOutOfOperationsRepackBoxResponse.Failure(OperationsBundleErrors.NotFound);
        }

        if (await _tracking.GetParcelAsync(request.ParcelId, cancellationToken) is not { } parcel || parcel.BoxId != box.Id)
        {
            return TakeOutOfOperationsRepackBoxResponse.Failure(OperationsBundleErrors.NotInBundle);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            box.Release(parcel.WeightKg ?? 0m, utcNow);
        }
        catch (DomainException exception)
        {
            return TakeOutOfOperationsRepackBoxResponse.Failure(exception.Error);
        }

        parcel.Unbox(utcNow);
        await _bundling.VoidLatestAsync(parcel, OperationsTrackingCodes.Boxed, "taken_out", access!.User.Id, cancellationToken);

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return TakeOutOfOperationsRepackBoxResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return TakeOutOfOperationsRepackBoxResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
