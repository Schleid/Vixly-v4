using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Supplies;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.CancelOperationsSupplyOrder;

// =========================================================
// FILE: CancelOperationsSupplyOrderUseCase.cs
//
// PURPOSE:
// An order of supplies cancelled at the counter (Slice 5E, S-15): a confirmed one puts its stock back.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.counter.serve, for the warehouse of the order
// - Slice 5F (I-7): a confirmed order with its invoice is cancelled by the approved void of that invoice, never here
// =========================================================
public sealed class CancelOperationsSupplyOrderUseCase
{
    private readonly OperationsSupplyCounter _counter;

    private readonly OperationsSupplySales _sales;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsBillingRepository _billing;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public CancelOperationsSupplyOrderUseCase(
        OperationsSupplyCounter counter,
        OperationsSupplySales sales,
        OperationsAuditTrail audit,
        IOperationsBillingRepository billing,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _counter = counter ?? throw new ArgumentNullException(nameof(counter));
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CancelOperationsSupplyOrderResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CancelOperationsSupplyOrderRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsSupplyCounter.Opened? opened, Error? refused) = await _counter.SaleAsync(context, request.SaleId, cancellationToken);

        if (refused is not null || opened!.Sale.ShipmentId is not null)
        {
            return CancelOperationsSupplyOrderResponse.Failure(refused ?? OperationsSupplySaleErrors.NotFound);
        }

        if (opened.Sale.Status == OperationsSupplySaleStatus.Cancelled)
        {
            return CancelOperationsSupplyOrderResponse.Success();
        }

        bool confirmed = opened.Sale.Status == OperationsSupplySaleStatus.Confirmed;

        if (confirmed && await _billing.FindOrderInvoiceAsync(opened.Sale.Id, cancellationToken) is not null)
        {
            return CancelOperationsSupplyOrderResponse.Failure(Vixly.Operations.Domain.Billing.OperationsBillingErrors.VoidRequired);
        }

        try
        {
            await _sales.CancelAsync(opened.Sale, opened.Access.User.Id, _timeProvider.GetUtcNow(), cancellationToken);
        }
        catch (DomainException exception)
        {
            return CancelOperationsSupplyOrderResponse.Failure(exception.Error);
        }

        _audit.Record(opened.Access.User.Id, "ops.supplies.order_cancelled", nameof(OperationsSupplySale), opened.Sale.Id,
            after: new { number = opened.Sale.Number, confirmed, total = opened.Sale.Total });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? CancelOperationsSupplyOrderResponse.Success()
            : CancelOperationsSupplyOrderResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
