using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.UseCases.Customers.ListOperationsCustomers;

// =========================================================
// FILE: ListOperationsCustomersUseCase.cs
//
// PURPOSE:
// Staff list the customers: search by name, email, phone or code, 25 per page (Slice 2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ListOperationsCustomersUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsCustomerRepository _customers;

    public ListOperationsCustomersUseCase(
        OperationsAccess access,
        IOperationsCustomerRepository customers)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
    }

    public async Task<ListOperationsCustomersResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsCustomersRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.CustomersView, false, cancellationToken);

        if (refused is not null)
        {
            return ListOperationsCustomersResponse.Failure(refused);
        }

        (IReadOnlyList<OperationsCustomer> items, int total) = await _customers.ListAsync(request.Search, Math.Max(request.Page, 1), PageSize, cancellationToken);
        return ListOperationsCustomersResponse.Success(items.Select(item => OperationsCustomerProfile.Of(item, Array.Empty<OperationsMailboxAddress>())).ToList(), total);
    }

    public const int PageSize = 25;
}
