using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Payables;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Payables;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Payables.GetOperationsProfit;

// =========================================================
// FILE: GetOperationsProfitUseCase.cs
//
// PURPOSE:
// The profit per shipment over a period (Slice 8): revenue, carrier cost, other costs and profit, with the totals.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.costs.view (company-wide)
// - At most a year at a time, at most 2 000 shipments
// =========================================================
public sealed class GetOperationsProfitUseCase
{
    private readonly OperationsPayablesDesk _desk;

    private readonly OperationsProfitCalculator _profit;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public GetOperationsProfitUseCase(
        OperationsPayablesDesk desk,
        OperationsProfitCalculator profit,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _profit = profit ?? throw new ArgumentNullException(nameof(profit));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsProfitResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsProfitRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? _, Error? refused) = await _desk.RequireAsync(context, OperationsPermissions.CostsView, false, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsProfitResponse.Failure(refused);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        DateOnly today = OperationsTimeZones.Today(_timeProvider.GetUtcNow(), settings.TimeZone);
        DateOnly from = request.From ?? new DateOnly(today.Year, today.Month, 1);
        DateOnly to = request.To ?? today;

        if (to < from || to.DayNumber - from.DayNumber > 366)
        {
            return GetOperationsProfitResponse.Failure(OperationsPayableErrors.DatesInvalid);
        }

        IReadOnlyList<Guid> shipments = await _desk.Payables.ListInvoicedShipmentsAsync(from, to, 2000, cancellationToken);
        return GetOperationsProfitResponse.Success(await _profit.ForAsync(shipments, cancellationToken), from, to, settings.CurrencyCode);
    }
}
