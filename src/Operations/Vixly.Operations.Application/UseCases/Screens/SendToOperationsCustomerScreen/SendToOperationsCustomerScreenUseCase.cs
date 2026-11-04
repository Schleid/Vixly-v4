using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Screens;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Screens;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Screens.SendToOperationsCustomerScreen;

// =========================================================
// FILE: SendToOperationsCustomerScreenUseCase.cs
//
// PURPOSE:
// Sends an invoice to pay or a shipment to sign to a customer screen, or clears it for the next customer (Slice 5F step 3, P-2, P-3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - An invoice needs ops.payments.take, a shipment to sign ops.counter.serve, at the screen’s warehouse
// - A new send replaces what was shown
// =========================================================
public sealed class SendToOperationsCustomerScreenUseCase
{
    private readonly OperationsScreenDesk _desk;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SendToOperationsCustomerScreenUseCase(
        OperationsScreenDesk desk,
        IOperationsBillingRepository billing,
        IOperationsReceivingRepository receiving,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SendToOperationsCustomerScreenResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SendToOperationsCustomerScreenRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return SendToOperationsCustomerScreenResponse.Failure(refused);
        }

        if (await _desk.Screens.GetAsync(request.ScreenId, cancellationToken) is not { } screen || warehouses.FirstOrDefault(item => item.Id == screen.WarehouseId) is not { } at)
        {
            return SendToOperationsCustomerScreenResponse.Failure(OperationsScreenErrors.NotFound);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        if (request.Clear)
        {
            screen.Clear(utcNow);
        }
        else if (request.InvoiceId is { } invoiceId)
        {
            // An invoice of this warehouse, to someone who takes payments here (partners only where they handle money).
            if (await _billing.GetInvoiceAsync(invoiceId, cancellationToken) is not { Status: OperationsInvoiceStatus.Issued, Owed: > 0m } invoice || invoice.WarehouseId != at.Id
                || !access!.CoversWarehouse(OperationsPermissions.PaymentsTake, at.Id, at.BranchId) || !access.HandlesMoneyAt(at))
            {
                return SendToOperationsCustomerScreenResponse.Failure(OperationsScreenErrors.NotFound);
            }

            screen.ShowInvoice(invoice.Id, access.User.Id, utcNow);
        }
        else if (request.ShipmentId is { } shipmentId)
        {
            // A shipment taken in at this counter, still open, to someone who serves here.
            if (await _receiving.GetShipmentAsync(shipmentId, cancellationToken) is not { HasInstructions: true } shipment || shipment.OriginWarehouseId != at.Id
                || !access!.CoversWarehouse(OperationsPermissions.CounterServe, at.Id, at.BranchId))
            {
                return SendToOperationsCustomerScreenResponse.Failure(OperationsScreenErrors.NotFound);
            }

            screen.ShowSignature(shipment.Id, access.User.Id, utcNow);
        }
        else
        {
            return SendToOperationsCustomerScreenResponse.Failure(OperationsScreenErrors.StepInvalid);
        }

        _audit.Record(access!.User.Id, "ops.screens.sent", nameof(OperationsCustomerScreen), screen.Id,
            after: new { step = screen.Step.ToString(), invoice = screen.InvoiceId, shipment = screen.ShipmentId });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SendToOperationsCustomerScreenResponse.Success()
            : SendToOperationsCustomerScreenResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
