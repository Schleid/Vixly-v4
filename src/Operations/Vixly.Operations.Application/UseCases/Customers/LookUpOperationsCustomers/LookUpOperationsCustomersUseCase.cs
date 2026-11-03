using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Customers;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Customers.LookUpOperationsCustomers;

// =========================================================
// FILE: LookUpOperationsCustomersUseCase.cs
//
// PURPOSE:
// A person without the company's customer list finds the one customer they are serving (owner, 2026-10-29, design 4.4, R-4).
//
// RESPONSIBILITIES:
// - Allow it to whoever holds ops.customers.lookup at any level, or the customer list for the whole company
// - Find by exact phone, email or code; record and limit through OperationsCustomerLookups
//
// IMPORTANT NOTES:
// - Example: Jean ships from the US to himself in Haiti; staff in both places can find him, neither can list the customers
// =========================================================
public sealed class LookUpOperationsCustomersUseCase
{
    public const int Take = 5;

    private readonly OperationsAccess _access;

    private readonly OperationsCustomerLookups _lookups;

    public LookUpOperationsCustomersUseCase(OperationsAccess access, OperationsCustomerLookups lookups)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _lookups = lookups ?? throw new ArgumentNullException(nameof(lookups));
    }

    public async Task<LookUpOperationsCustomersResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        LookUpOperationsCustomersRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _access.LoadAsync(context, cancellationToken) is not { } access)
        {
            return LookUpOperationsCustomersResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (!access.Has(OperationsPermissions.CustomersLookup) && !access.HasTenantWide(OperationsPermissions.CustomersView))
        {
            return LookUpOperationsCustomersResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        (IReadOnlyList<OperationsCustomer> found, Error? limit) = await _lookups.FindAsync(access, request.Text, Take, cancellationToken);

        return limit is not null
            ? LookUpOperationsCustomersResponse.Failure(limit)
            : LookUpOperationsCustomersResponse.Success(found.Select(customer => OperationsCustomerProfile.Of(customer, Array.Empty<Vixly.Operations.Application.Customers.OperationsMailboxAddress>())).ToList());
    }
}
