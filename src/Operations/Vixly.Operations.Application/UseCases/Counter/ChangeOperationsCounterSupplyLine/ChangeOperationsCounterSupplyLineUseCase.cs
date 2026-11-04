using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Supplies;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.ChangeOperationsCounterSupplyLine;

// =========================================================
// FILE: ChangeOperationsCounterSupplyLineUseCase.cs
//
// PURPOSE:
// A line of supplies changed at the counter (Slice 5E, S-11, S-17 to S-19): its quantity (0 removes it) and its discount within the company's policy.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.counter.serve, for the warehouse of the sale
// - The form's key: sent twice, applied once (ADR-0019)
// - A discount is always below 100%; every one is kept with who gave it, who approved it and why (S-19)
// =========================================================
public sealed class ChangeOperationsCounterSupplyLineUseCase
{
    private readonly OperationsSupplyCounter _counter;

    private readonly IOperationsSupplyRepository _supplies;

    private readonly OperationsSupplySales _sales;

    private readonly OperationsSupplyDiscounts _discounts;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ChangeOperationsCounterSupplyLineUseCase(
        OperationsSupplyCounter counter,
        IOperationsSupplyRepository supplies,
        OperationsSupplySales sales,
        OperationsSupplyDiscounts discounts,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _counter = counter ?? throw new ArgumentNullException(nameof(counter));
        _supplies = supplies ?? throw new ArgumentNullException(nameof(supplies));
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _discounts = discounts ?? throw new ArgumentNullException(nameof(discounts));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ChangeOperationsCounterSupplyLineResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChangeOperationsCounterSupplyLineRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsSupplyCounter.Opened? opened, Error? refused) = await _counter.SaleAsync(context, request.SaleId, cancellationToken);

        if (refused is not null || OperationsSupplyCounter.Changeable(opened!) is { } closed && (refused = closed) is not null)
        {
            return ChangeOperationsCounterSupplyLineResponse.Failure(refused!);
        }

        OperationsSupplySale sale = opened!.Sale;

        if (sale.Applied(request.Key))
        {
            return ChangeOperationsCounterSupplyLineResponse.Success(false);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        bool short_ = false;
        OperationsSupplySaleLine line;

        try
        {
            line = sale.Line(request.LineId);
            decimal before = line.DiscountPercent;

            if (request.Quantity == 0)
            {
                sale.Remove(line.Id, utcNow);
                _audit.Record(opened.Access.User.Id, "ops.supplies.sale_line_removed", nameof(OperationsSupplySale), sale.Id, before: new { supply = line.Code, quantity = line.Quantity });
            }
            else
            {
                if (request.Quantity != line.Quantity)
                {
                    OperationsSupply supply = await _supplies.GetAsync(line.SupplyId, cancellationToken) ?? throw new DomainException(OperationsSupplyErrors.NotFound);
                    short_ = await _sales.CountAsync(sale, supply, line.Id, request.Quantity, opened.Settings, utcNow, cancellationToken);
                }

                // The discount is checked against the policy with the quantity as it now stands.
                if (request.DiscountPercent != before || (request.DiscountPercent != 0m && request.DiscountReason?.Trim() != line.DiscountReason))
                {
                    (Guid? approvedBy, Error? notAllowed) = await _discounts.CheckAsync(sale, line.Id, request.DiscountPercent, request.DiscountReason, opened.Access, opened.Warehouse,
                        opened.Settings, request.ApproverEmail, request.ApproverCode, cancellationToken);

                    if (notAllowed is not null)
                    {
                        // A refused approval changes nothing on the sale; the failure is recorded against the approver and in the history (review of 5E).
                        if (notAllowed == OperationsSupplySaleErrors.ApproverInvalid)
                        {
                            string code = line.Code;
                            _unitOfWork.DiscardChanges();
                            await _discounts.RecordFailureAsync(request.ApproverEmail, cancellationToken);
                            _audit.Record(opened.Access.User.Id, "ops.supplies.discount_refused", nameof(OperationsSupplySale), sale.Id,
                                after: new { supply = code, percent = request.DiscountPercent, approver = request.ApproverEmail });
                            await _unitOfWork.SaveChangesAsync(cancellationToken);
                        }

                        return ChangeOperationsCounterSupplyLineResponse.Failure(notAllowed);
                    }

                    sale.Discount(line.Id, request.DiscountPercent, request.DiscountReason, opened.Access.User.Id, approvedBy, utcNow);
                    _audit.Record(opened.Access.User.Id, "ops.supplies.discount_given", nameof(OperationsSupplySale), sale.Id,
                        before: new { percent = before }, after: new { supply = line.Code, percent = request.DiscountPercent, reason = request.DiscountReason, approvedBy });
                }
            }
        }
        catch (DomainException exception)
        {
            return ChangeOperationsCounterSupplyLineResponse.Failure(exception.Error);
        }

        // A quantity changed or a line removed: the order's other discounts must still keep to the policy (review of 5E).
        if (!OperationsSupplyDiscounts.Within(sale, opened.Settings))
        {
            return ChangeOperationsCounterSupplyLineResponse.Failure(OperationsSupplySaleErrors.DiscountNeedsApproval);
        }

        sale.Keyed(request.Key);
        opened.Shipment?.SuppliesChanged(utcNow);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ChangeOperationsCounterSupplyLineResponse.Success(short_)
            : ChangeOperationsCounterSupplyLineResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
