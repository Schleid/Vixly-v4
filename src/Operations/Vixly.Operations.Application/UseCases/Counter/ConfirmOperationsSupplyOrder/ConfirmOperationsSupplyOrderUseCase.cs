using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Supplies;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.ConfirmOperationsSupplyOrder;

// =========================================================
// FILE: ConfirmOperationsSupplyOrderUseCase.cs
//
// PURPOSE:
// An order of supplies confirmed at the counter (Slice 5E, S-14 and S-15): it gets its number and its stock moves; it is then to pay.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.counter.serve, for the warehouse of the order
// - Slice 5F (F-1): its own invoice is issued with it; paid at the cashier (step 2)
// =========================================================
public sealed class ConfirmOperationsSupplyOrderUseCase
{
    private readonly OperationsSupplyCounter _counter;

    private readonly OperationsSupplySales _sales;

    private readonly OperationsAuditTrail _audit;

    private readonly Vixly.Operations.Application.Billing.OperationsInvoicing _invoicing;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ConfirmOperationsSupplyOrderUseCase(
        OperationsSupplyCounter counter,
        OperationsSupplySales sales,
        OperationsAuditTrail audit,
        Vixly.Operations.Application.Billing.OperationsInvoicing invoicing,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _counter = counter ?? throw new ArgumentNullException(nameof(counter));
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _invoicing = invoicing ?? throw new ArgumentNullException(nameof(invoicing));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ConfirmOperationsSupplyOrderResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ConfirmOperationsSupplyOrderRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsSupplyCounter.Opened? opened, Error? refused) = await _counter.SaleAsync(context, request.SaleId, cancellationToken);

        if (refused is not null || opened!.Sale.ShipmentId is not null)
        {
            return ConfirmOperationsSupplyOrderResponse.Failure(refused ?? OperationsSupplySaleErrors.NotFound);
        }

        // Confirmed twice (a double click): the number it already has.
        if (opened.Sale is { Status: OperationsSupplySaleStatus.Confirmed, Number: { } already })
        {
            return ConfirmOperationsSupplyOrderResponse.Success(already);
        }

        try
        {
            DateTimeOffset utcNow = _timeProvider.GetUtcNow();
            await _sales.ConfirmAsync(opened.Sale, opened.Access.User.Id, null, utcNow, cancellationToken);
            await _invoicing.IssueForOrderAsync(opened.Sale, utcNow, cancellationToken);
        }
        catch (DomainException exception)
        {
            return ConfirmOperationsSupplyOrderResponse.Failure(exception.Error);
        }

        _audit.Record(opened.Access.User.Id, "ops.supplies.order_confirmed", nameof(OperationsSupplySale), opened.Sale.Id,
            after: new { number = opened.Sale.Number, total = opened.Sale.Total, lines = opened.Sale.Lines.Count });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ConfirmOperationsSupplyOrderResponse.Success(opened.Sale.Number!)
            : ConfirmOperationsSupplyOrderResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
