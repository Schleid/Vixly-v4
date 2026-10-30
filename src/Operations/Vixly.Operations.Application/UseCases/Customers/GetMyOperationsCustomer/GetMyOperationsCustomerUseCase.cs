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

namespace Vixly.Operations.Application.UseCases.Customers.GetMyOperationsCustomer;

// =========================================================
// FILE: GetMyOperationsCustomerUseCase.cs
//
// PURPOSE:
// The signed-in customer's profile and mailbox addresses (Slice 2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetMyOperationsCustomerUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    public GetMyOperationsCustomerUseCase(
        OperationsCustomerAccounts accounts)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
    }

    public async Task<GetMyOperationsCustomerResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetMyOperationsCustomerRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        return await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is { } signedIn
            ? GetMyOperationsCustomerResponse.Success(await _accounts.ProfileAsync(signedIn.Customer, cancellationToken))
            : GetMyOperationsCustomerResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
    }
}
