using Vixly.Operations.Application.Counter;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Shipments;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.ChangeOperationsCounterParcelCount;

// =========================================================
// FILE: ChangeOperationsCounterParcelCountUseCase.cs
//
// PURPOSE:
// Corrects the parcel count of a shipment made at the counter, before its first parcel (Slice 5C, C-1).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.counter.serve at the shipment's origin
// - More boxes once parcels are in means a new shipment (owner, 2026-10-20)
// =========================================================
public sealed class ChangeOperationsCounterParcelCountUseCase
{
    private readonly OperationsCounterDesk _desk;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ChangeOperationsCounterParcelCountUseCase(
        OperationsCounterDesk desk,
        IOperationsReceivingRepository receiving,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ChangeOperationsCounterParcelCountResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChangeOperationsCounterParcelCountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? _, IReadOnlyList<OperationsWarehouse> counters, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return ChangeOperationsCounterParcelCountResponse.Failure(refused);
        }

        if (await _receiving.GetShipmentAsync(request.ShipmentId, cancellationToken) is not { } shipment)
        {
            return ChangeOperationsCounterParcelCountResponse.Failure(OperationsShipmentErrors.NotFound);
        }

        if (!counters.Any(counter => counter.Id == shipment.OriginWarehouseId))
        {
            return ChangeOperationsCounterParcelCountResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        // A portal shipment's count is the customer's statement; a counter shipment's, the clerk's until a parcel is in (C-1).
        // A shipment from before counts existed has none: the clerk records what the customer says at the counter (review).
        IReadOnlyList<Vixly.Operations.Domain.Tracking.OperationsParcel> parcels = Vixly.Operations.Application.Shipments.OperationsShipmentBooking.Counted(await _receiving.ListShipmentParcelsAsync(shipment.Id, cancellationToken));

        if (shipment.ParcelCount is not null && (!shipment.CreatedAtCounter || parcels.Count > 0))
        {
            return ChangeOperationsCounterParcelCountResponse.Failure(OperationsShipmentErrors.ParcelCountLocked);
        }

        try
        {
            shipment.DeclareParcelCount(request.ParcelCount, 0, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return ChangeOperationsCounterParcelCountResponse.Failure(exception.Error);
        }

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ChangeOperationsCounterParcelCountResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return ChangeOperationsCounterParcelCountResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
