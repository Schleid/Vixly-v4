using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Counter;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Supplies;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.StartOperationsSupplyOrder;

// =========================================================
// FILE: StartOperationsSupplyOrderUseCase.cs
//
// PURPOSE:
// An order of supplies on its own for a customer at the counter (Slice 5E, S-14): the open one, or a new one.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.counter.serve
// - A customer not in good standing cannot order
// =========================================================
public sealed class StartOperationsSupplyOrderUseCase
{
    private readonly OperationsCounterDesk _desk;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsSupplyRepository _supplies;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public StartOperationsSupplyOrderUseCase(
        OperationsCounterDesk desk,
        IOperationsCustomerRepository customers,
        IOperationsSupplyRepository supplies,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _supplies = supplies ?? throw new ArgumentNullException(nameof(supplies));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<StartOperationsSupplyOrderResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        StartOperationsSupplyOrderRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> counters, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return StartOperationsSupplyOrderResponse.Failure(refused);
        }

        // A customer in good standing only, as for a new shipment (owner, 2026-10-20).
        if (await _customers.GetAsync(request.CustomerId, cancellationToken) is not { } customer)
        {
            return StartOperationsSupplyOrderResponse.Failure(OperationsSupplySaleErrors.NotFound);
        }

        if (customer.Status != OperationsCustomerStatus.Active)
        {
            return StartOperationsSupplyOrderResponse.Failure(OperationsCustomerErrors.NotActive);
        }

        OperationsWarehouse warehouse = counters.FirstOrDefault(counter => counter.Id == request.WarehouseId) ?? counters[0];

        // One open order per customer at a counter: started twice (a double click), the one already there.
        if ((await _supplies.ListCustomerOrdersAsync(customer.Id, 20, cancellationToken)).FirstOrDefault(order => order.Status == OperationsSupplySaleStatus.Open && order.WarehouseId == warehouse.Id) is { } open)
        {
            return StartOperationsSupplyOrderResponse.Success(open.Id);
        }

        string currency = (await _settings.FindAsync(cancellationToken))?.CurrencyCode ?? OperationsTenantSettings.DefaultCurrency;
        OperationsSupplySale sale = OperationsSupplySale.Start(context.RequiredTenantId, customer.Id, warehouse.Id, null, currency, access!.User.Id, _timeProvider.GetUtcNow());
        _supplies.Add(sale);
        _audit.Record(access.User.Id, "ops.supplies.order_started", nameof(OperationsSupplySale), sale.Id, after: new { customer = customer.Code, warehouse = warehouse.Code });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? StartOperationsSupplyOrderResponse.Success(sale.Id)
            : StartOperationsSupplyOrderResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
