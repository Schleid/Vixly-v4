using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.CloseOperationsDelivery;

// =========================================================
// FILE: CloseOperationsDeliveryUseCase.cs
//
// PURPOSE:
// Closes a carrier delivery and issues its warehouse receipt (Slice 3B, owner 2026-10-18).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The piece and damage counts come from the delivery's parcels at closing
// =========================================================
public sealed class CloseOperationsDeliveryUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly OperationsNumbering _numbering;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public CloseOperationsDeliveryUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsReceivingRepository receiving,
        OperationsNumbering numbering,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CloseOperationsDeliveryResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CloseOperationsDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return CloseOperationsDeliveryResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _receiving.GetDeliveryAsync(request.DeliveryId, cancellationToken) is not { } delivery
            || await _scope.CoveredAsync(access, OperationsPermissions.ReceivingReceive, delivery.WarehouseId, cancellationToken) is null)
        {
            return CloseOperationsDeliveryResponse.Failure(OperationsReceivingErrors.DeliveryNotFound);
        }

        // A scan saved at the same moment changes the delivery row, so this close fails and counts again; another device's close stands.
        for (int attempt = 0; attempt < 3; attempt++)
        {
            if (attempt > 0)
            {
                _unitOfWork.DiscardChanges();

                if (await _receiving.GetDeliveryAsync(request.DeliveryId, cancellationToken) is not { } reloaded)
                {
                    return CloseOperationsDeliveryResponse.Failure(OperationsReceivingErrors.DeliveryNotFound);
                }

                delivery = reloaded;
            }

            // Closed already (a double click, another device): the same receipt.
            if (delivery.ReceiptId is { } issued && await _receiving.GetReceiptAsync(issued, cancellationToken) is { } existing)
            {
                return CloseOperationsDeliveryResponse.Success(existing.Id, existing.Number);
            }

            DateTimeOffset utcNow = _timeProvider.GetUtcNow();
            IReadOnlyList<OperationsParcel> parcels = await _receiving.ListDeliveryParcelsAsync(delivery.Id, cancellationToken);
            OperationsWarehouseReceipt receipt = OperationsWarehouseReceipt.Issue(context.RequiredTenantId, await _numbering.NextAsync(OperationsNumberKind.Receiving, cancellationToken), delivery,
                access.User.Id, parcels.Count, parcels.Count(parcel => parcel.ArrivedDamaged), utcNow);
            _receiving.Add(receipt);
            delivery.Close(receipt.Id, utcNow);
            _audit.Record(access.User.Id, "ops.receiving.delivery_closed", nameof(OperationsWarehouseReceipt), receipt.Id,
                after: new { number = receipt.Number, pieces = receipt.PieceCount, damaged = receipt.DamagedCount, carrier = receipt.Carrier });

            if (await _unitOfWork.SaveChangesAsync(cancellationToken))
            {
                return CloseOperationsDeliveryResponse.Success(receipt.Id, receipt.Number);
            }
        }

        return CloseOperationsDeliveryResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
