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

namespace Vixly.Operations.Application.UseCases.Bundles.VoidOperationsBundle;

// =========================================================
// FILE: VoidOperationsBundleUseCase.cs
//
// PURPOSE:
// Void a bundle with a reason (Slice 6A, D-8; owner, A-4): its parcels and boxes are ready to bundle again.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bundles.manage, at its warehouse
// - An empty open bundle, or a closed one until it is on a manifest (manifests come with Slice 6B)
// =========================================================
public sealed class VoidOperationsBundleUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsBundleRepository _bundles;

    private readonly OperationsBundling _bundling;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public VoidOperationsBundleUseCase(
        OperationsBundleDesk desk,
        IOperationsBundleRepository bundles,
        OperationsBundling bundling,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _bundling = bundling ?? throw new ArgumentNullException(nameof(bundling));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<VoidOperationsBundleResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        VoidOperationsBundleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.BundlesManage }, cancellationToken);

        if (refused is not null)
        {
            return VoidOperationsBundleResponse.Failure(refused);
        }

        if (await _bundles.GetBundleAsync(request.BundleId, cancellationToken) is not { } bundle || warehouses.All(item => item.Id != bundle.WarehouseId))
        {
            return VoidOperationsBundleResponse.Failure(OperationsBundleErrors.NotFound);
        }

        if (bundle.Status == OperationsBundleStatus.Voided)
        {
            return VoidOperationsBundleResponse.Success();
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        IReadOnlyList<OperationsParcel> parcels = await _bundles.ListBundleParcelsForUpdateAsync(bundle.Id, cancellationToken);
        IReadOnlyList<OperationsRepackBox> boxes = await _bundles.ListBundleBoxesForUpdateAsync(bundle.Id, cancellationToken);
        OperationsBundleStatus before = bundle.Status;

        try
        {
            bundle.Void(request.Reason, access!.User.Id, utcNow);
        }
        catch (DomainException exception)
        {
            return VoidOperationsBundleResponse.Failure(exception.Error);
        }

        foreach (OperationsRepackBox box in boxes)
        {
            box.TakeOut(utcNow);
        }

        foreach (OperationsParcel parcel in parcels)
        {
            parcel.Unpack(utcNow);
            await _bundling.VoidLatestAsync(parcel, OperationsTrackingCodes.Packed, "bundle_voided", access.User.Id, cancellationToken);
        }

        await _bundling.BundleEventAsync(bundle, OperationsTrackingCodes.BundleVoided, new Dictionary<string, string>(StringComparer.Ordinal), access.User.Id, bundle.VoidReason, cancellationToken);
        _audit.Record(access.User.Id, "ops.bundles.voided", nameof(OperationsBundle), bundle.Id, before: new { status = before.ToString(), parcels = parcels.Count },
            after: new { status = bundle.Status.ToString() }, reason: bundle.VoidReason);

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return VoidOperationsBundleResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return VoidOperationsBundleResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
