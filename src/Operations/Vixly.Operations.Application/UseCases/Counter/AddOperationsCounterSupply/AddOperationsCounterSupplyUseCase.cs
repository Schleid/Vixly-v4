using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Supplies;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.AddOperationsCounterSupply;

// =========================================================
// FILE: AddOperationsCounterSupplyUseCase.cs
//
// PURPOSE:
// A supply added at the counter (Slice 5E, S-11): by scan or by name, to the shipment being processed or to an order of supplies.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.counter.serve, for the warehouse of the sale
// - The form's key: sent twice, applied once (ADR-0019)
// - A shipment's supplies only before it is final (S-13)
// =========================================================
public sealed class AddOperationsCounterSupplyUseCase
{
    private readonly OperationsSupplyCounter _counter;

    private readonly IOperationsSupplyRepository _supplies;

    private readonly OperationsSupplySales _sales;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public AddOperationsCounterSupplyUseCase(
        OperationsSupplyCounter counter,
        IOperationsSupplyRepository supplies,
        OperationsSupplySales sales,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _counter = counter ?? throw new ArgumentNullException(nameof(counter));
        _supplies = supplies ?? throw new ArgumentNullException(nameof(supplies));
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<AddOperationsCounterSupplyResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        AddOperationsCounterSupplyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsSupplyCounter.Opened? opened, Error? refused) = request.SaleId is { } saleId
            ? await _counter.SaleAsync(context, saleId, cancellationToken)
            : request.ShipmentId is { } shipmentId
                ? await _counter.ShipmentAsync(context, shipmentId, true, context.UserId ?? Guid.Empty, cancellationToken)
                : (null, OperationsSupplySaleErrors.NotFound);

        if (refused is not null || OperationsSupplyCounter.Changeable(opened!) is { } closed && (refused = closed) is not null)
        {
            return AddOperationsCounterSupplyResponse.Failure(refused!);
        }

        OperationsSupplySale sale = opened!.Sale;

        if (sale.Applied(request.Key))
        {
            return AddOperationsCounterSupplyResponse.Success(sale.Id, true, false, null, Array.Empty<OperationsSupply>());
        }

        // A scan finds one supply by barcode or code; a text the scanner did not send may name several.
        OperationsSupply? supply = request.SupplyId is { } chosen ? await _supplies.GetAsync(chosen, cancellationToken)
            : OperationsSupplyLimits.TryCode(request.Scan, OperationsSupplyLimits.BarcodeMaxLength) is { } code ? await _supplies.FindByScanAsync(code, cancellationToken)
            : null;

        if (supply is null)
        {
            IReadOnlyList<OperationsSupply> found = string.IsNullOrWhiteSpace(request.Scan) || request.Scan.Trim().Length > 80 ? Array.Empty<OperationsSupply>()
                : await _supplies.SearchAsync(request.Scan, 10, cancellationToken);

            if (found.Count != 1)
            {
                return found.Count == 0
                    ? AddOperationsCounterSupplyResponse.Failure(OperationsSupplySaleErrors.NotFoundByScan)
                    : AddOperationsCounterSupplyResponse.Success(sale.Id, false, false, null, found);
            }

            supply = found[0];
        }

        if (!supply.IsActive)
        {
            return AddOperationsCounterSupplyResponse.Failure(OperationsSupplySaleErrors.NotFoundByScan);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        bool short_;

        try
        {
            short_ = await _sales.AddAsync(sale, supply, request.Quantity, opened.Settings, utcNow, cancellationToken);
            opened.Shipment?.SuppliesChanged(utcNow);
        }
        catch (DomainException exception)
        {
            return AddOperationsCounterSupplyResponse.Failure(exception.Error);
        }

        // More of the order: its discounts must still keep to the policy (review of 5E).
        if (!OperationsSupplyDiscounts.Within(sale, opened.Settings))
        {
            return AddOperationsCounterSupplyResponse.Failure(OperationsSupplySaleErrors.DiscountNeedsApproval);
        }

        sale.Keyed(request.Key);
        _audit.Record(opened.Access.User.Id, "ops.supplies.sale_line_added", nameof(OperationsSupplySale), sale.Id,
            after: new { supply = supply.Code, quantity = request.Quantity, shipment = opened.Shipment?.Number, warning = short_ });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? AddOperationsCounterSupplyResponse.Success(sale.Id, true, short_, supply.Code, Array.Empty<OperationsSupply>())
            : AddOperationsCounterSupplyResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
