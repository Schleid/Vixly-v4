using Vixly.Operations.Application.Audit;
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

namespace Vixly.Operations.Application.UseCases.Bundles.VoidOperationsRepackBox;

// =========================================================
// FILE: VoidOperationsRepackBoxUseCase.cs
//
// PURPOSE:
// Void a repack box in no bundle, with a reason (Slice 6A, D-4): its parcels are loose again.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bundles.manage, at its warehouse
// - Not while it is in a bundle: void or take it out of the bundle first
// =========================================================
public sealed class VoidOperationsRepackBoxUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsBundleRepository _bundles;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly OperationsBundling _bundling;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public VoidOperationsRepackBoxUseCase(
        OperationsBundleDesk desk,
        IOperationsBundleRepository bundles,
        IOperationsReceivingRepository receiving,
        OperationsBundling bundling,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _bundling = bundling ?? throw new ArgumentNullException(nameof(bundling));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<VoidOperationsRepackBoxResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        VoidOperationsRepackBoxRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.BundlesManage }, cancellationToken);

        if (refused is not null)
        {
            return VoidOperationsRepackBoxResponse.Failure(refused);
        }

        if (await _bundles.GetBoxAsync(request.BoxId, cancellationToken) is not { } box || warehouses.All(item => item.Id != box.WarehouseId))
        {
            return VoidOperationsRepackBoxResponse.Failure(OperationsBundleErrors.NotFound);
        }

        if (box.Status == OperationsRepackBoxStatus.Voided)
        {
            return VoidOperationsRepackBoxResponse.Success();
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        List<OperationsParcel> inside = (await _receiving.ListShipmentParcelsForUpdateAsync(box.ShipmentId, cancellationToken)).Where(parcel => parcel.BoxId == box.Id).ToList();

        try
        {
            box.Void(request.Reason, access!.User.Id, utcNow);
        }
        catch (DomainException exception)
        {
            return VoidOperationsRepackBoxResponse.Failure(exception.Error);
        }

        foreach (OperationsParcel parcel in inside)
        {
            parcel.Unbox(utcNow);
            await _bundling.VoidLatestAsync(parcel, OperationsTrackingCodes.Boxed, "box_voided", access.User.Id, cancellationToken);
        }

        _audit.Record(access.User.Id, "ops.bundles.box_voided", nameof(OperationsRepackBox), box.Id, before: new { parcels = inside.Count }, reason: box.VoidReason);

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return VoidOperationsRepackBoxResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return VoidOperationsRepackBoxResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
