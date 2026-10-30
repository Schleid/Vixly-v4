using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.ListOperationsCashDrawers;

// =========================================================
// FILE: ListOperationsCashDrawersUseCase.cs
//
// PURPOSE:
// Cash drawers (Slice 5F, C-4): the person's own, and every drawer of the warehouses where they confirm payments.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.payments.take for one's own; ops.payments.confirm for every drawer of the warehouse
// =========================================================
public sealed class ListOperationsCashDrawersUseCase
{
    private readonly OperationsBillingDesk _desk;

    private readonly IOperationsPaymentRepository _payments;

    private readonly IOperationsUserRepository _users;

    public ListOperationsCashDrawersUseCase(
        OperationsBillingDesk desk,
        IOperationsPaymentRepository payments,
        IOperationsUserRepository users)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _users = users ?? throw new ArgumentNullException(nameof(users));
    }

    public async Task<ListOperationsCashDrawersResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsCashDrawersRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> take, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.PaymentsTake, cancellationToken);

        if (access is null)
        {
            return ListOperationsCashDrawersResponse.Failure(refused ?? OperationsAccessErrors.SignInRequired);
        }

        IReadOnlyList<OperationsWarehouse> confirm = await _desk.WarehousesAsync(access, OperationsPermissions.PaymentsConfirm, cancellationToken);

        if (take.Count == 0 && confirm.Count == 0)
        {
            return ListOperationsCashDrawersResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        HashSet<Guid> all = confirm.Select(warehouse => warehouse.Id).ToHashSet();
        List<OperationsWarehouse> places = take.Concat(confirm).DistinctBy(warehouse => warehouse.Id).ToList();
        IReadOnlyList<OperationsCashDrawer> drawers = (await _payments.ListDrawersAsync(places.Select(warehouse => warehouse.Id).ToList(), 100, cancellationToken))
            .Where(drawer => drawer.UserId == access.User.Id || all.Contains(drawer.WarehouseId)).ToList();
        Dictionary<Guid, string> people = new();

        foreach (Guid userId in drawers.Select(drawer => drawer.UserId).Distinct())
        {
            people[userId] = (await _users.GetAsync(userId, cancellationToken))?.FullName ?? string.Empty;
        }

        return ListOperationsCashDrawersResponse.Success(drawers, take, people, access.User.Id);
    }
}
