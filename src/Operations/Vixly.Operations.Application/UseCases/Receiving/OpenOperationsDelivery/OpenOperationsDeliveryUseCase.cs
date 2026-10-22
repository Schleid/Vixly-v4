using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.OpenOperationsDelivery;

// =========================================================
// FILE: OpenOperationsDeliveryUseCase.cs
//
// PURPOSE:
// Opens a carrier delivery at the dock: the packages one driver brings (Slice 3B, owner 2026-10-18).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Several deliveries may be open at once at one warehouse (several docks)
// =========================================================
public sealed class OpenOperationsDeliveryUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public OpenOperationsDeliveryUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsReceivingRepository receiving,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<OpenOperationsDeliveryResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        OpenOperationsDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return OpenOperationsDeliveryResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _scope.CoveredAsync(access, OperationsPermissions.ReceivingReceive, request.WarehouseId, cancellationToken) is not { } warehouse)
        {
            return OpenOperationsDeliveryResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        if (warehouse.Status != OperationsLocationStatus.Active || !warehouse.Roles.HasFlag(OperationsWarehouseRoles.Receiving))
        {
            return OpenOperationsDeliveryResponse.Failure(OperationsReceivingErrors.WarehouseNotReceiving);
        }

        if (!OperationsCommandKey.IsValid(request.CommandKey))
        {
            return OpenOperationsDeliveryResponse.Failure(OperationsTrackingErrors.IdempotencyKeyInvalid);
        }

        // The same form sent twice opens one delivery (ADR-0019).
        if (await _receiving.FindDeliveryByOpenKeyAsync(request.CommandKey, cancellationToken) is { } earlier)
        {
            return OpenOperationsDeliveryResponse.Success(earlier.Id);
        }

        OperationsInboundDelivery delivery;

        try
        {
            delivery = OperationsInboundDelivery.Open(context.RequiredTenantId, warehouse.Id, request.Carrier, access.User.Id, request.CommandKey, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return OpenOperationsDeliveryResponse.Failure(exception.Error);
        }

        _receiving.Add(delivery);

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return OpenOperationsDeliveryResponse.Success(delivery.Id);
        }

        _unitOfWork.DiscardChanges();
        return await _receiving.FindDeliveryByOpenKeyAsync(request.CommandKey, cancellationToken) is { } raced
            ? OpenOperationsDeliveryResponse.Success(raced.Id)
            : OpenOperationsDeliveryResponse.Failure(Vixly.Operations.Domain.Common.OperationsCommonErrors.ConcurrencyConflict);
    }
}
