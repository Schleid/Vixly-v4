using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.GetOperationsCashDrawer;

// =========================================================
// FILE: GetOperationsCashDrawerUseCase.cs
//
// PURPOSE:
// A cash drawer's report (Slice 5F, C-4): float, cash in, cash out, expected, counted, difference, and each payment and refund in cash.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One's own drawer, or any drawer of a warehouse where one confirms payments
// =========================================================
public sealed class GetOperationsCashDrawerUseCase
{
    private readonly OperationsBillingDesk _desk;

    private readonly IOperationsPaymentRepository _payments;

    private readonly IOperationsUserRepository _users;

    public GetOperationsCashDrawerUseCase(
        OperationsBillingDesk desk,
        IOperationsPaymentRepository payments,
        IOperationsUserRepository users)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _users = users ?? throw new ArgumentNullException(nameof(users));
    }

    public async Task<GetOperationsCashDrawerResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsCashDrawerRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> _, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.PaymentsTake, cancellationToken);

        if (access is null)
        {
            return GetOperationsCashDrawerResponse.Failure(refused ?? OperationsAccessErrors.SignInRequired);
        }

        IReadOnlyList<OperationsWarehouse> confirm = await _desk.WarehousesAsync(access, OperationsPermissions.PaymentsConfirm, cancellationToken);

        if (await _payments.GetDrawerAsync(request.DrawerId, cancellationToken) is not { } drawer
            || (drawer.UserId != access.User.Id && confirm.All(warehouse => warehouse.Id != drawer.WarehouseId)))
        {
            return GetOperationsCashDrawerResponse.Failure(OperationsPaymentErrors.DrawerNotFound);
        }

        return GetOperationsCashDrawerResponse.Success(drawer, await _payments.ListDrawerPaymentsAsync(drawer.Id, cancellationToken),
            await _payments.ListDrawerRefundsAsync(drawer.Id, cancellationToken), (await _users.GetAsync(drawer.UserId, cancellationToken))?.FullName ?? string.Empty,
            drawer.UserId == access.User.Id);
    }
}
