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

namespace Vixly.Operations.Application.UseCases.Payables.GetOperationsShipmentProfit;

// =========================================================
// FILE: GetOperationsShipmentProfitUseCase.cs
//
// PURPOSE:
// One shipment’s costs and profit, for its page (Slice 8).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.costs.view; refused quietly for the others (the page shows nothing)
// =========================================================
public sealed class GetOperationsShipmentProfitUseCase
{
    private readonly OperationsPayablesDesk _desk;

    private readonly OperationsProfitCalculator _profit;

    public GetOperationsShipmentProfitUseCase(
        OperationsPayablesDesk desk,
        OperationsProfitCalculator profit)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _profit = profit ?? throw new ArgumentNullException(nameof(profit));
    }

    public async Task<GetOperationsShipmentProfitResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsShipmentProfitRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? _, Error? refused) = await _desk.RequireAsync(context, OperationsPermissions.CostsView, false, cancellationToken);

        return refused is not null
            ? GetOperationsShipmentProfitResponse.Failure(refused)
            : GetOperationsShipmentProfitResponse.Success((await _profit.ForAsync(new[] { request.ShipmentId }, cancellationToken)).FirstOrDefault());
    }
}
