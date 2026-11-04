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

namespace Vixly.Operations.Application.UseCases.Bundles.ChooseOperationsConsolidation;

// =========================================================
// FILE: ChooseOperationsConsolidationUseCase.cs
//
// PURPOSE:
// Switch how a consolidated shipment travels: repacked or kept together (Slice 6A, D-4; owner, A-3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bundles.manage, at the shipment's origin warehouse
// - Only before any of its parcels is boxed or bundled
// =========================================================
public sealed class ChooseOperationsConsolidationUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsBundleRepository _bundles;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ChooseOperationsConsolidationUseCase(
        OperationsBundleDesk desk,
        IOperationsBundleRepository bundles,
        IOperationsReceivingRepository receiving,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ChooseOperationsConsolidationResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChooseOperationsConsolidationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.BundlesManage }, cancellationToken);

        if (refused is not null)
        {
            return ChooseOperationsConsolidationResponse.Failure(refused);
        }

        if (await _receiving.GetShipmentAsync(request.ShipmentId, cancellationToken) is not { } shipment || warehouses.All(item => item.Id != shipment.OriginWarehouseId))
        {
            return ChooseOperationsConsolidationResponse.Failure(OperationsBundleErrors.NotFound);
        }

        // Once a parcel is boxed or bundled, the way is fixed: void the box or the bundle first.
        if ((await _receiving.ListShipmentParcelsAsync(shipment.Id, cancellationToken)).Any(parcel => parcel.BoxId is not null || parcel.BundleId is not null)
            || await _bundles.FindLiveBoxAsync(shipment.Id, cancellationToken) is not null)
        {
            return ChooseOperationsConsolidationResponse.Failure(OperationsBundleErrors.WayLocked);
        }

        OperationsConsolidationWay? before = shipment.ConsolidationWay;

        try
        {
            shipment.ChooseConsolidation(request.Way, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return ChooseOperationsConsolidationResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, "ops.bundles.way_chosen", nameof(OperationsShipment), shipment.Id, before: new { way = before?.ToString() }, after: new { way = request.Way.ToString() });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ChooseOperationsConsolidationResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return ChooseOperationsConsolidationResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
