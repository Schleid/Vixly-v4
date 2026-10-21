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

namespace Vixly.Operations.Application.UseCases.Customers.SignOutOperationsCustomer;

// =========================================================
// FILE: SignOutOperationsCustomerUseCase.cs
//
// PURPOSE:
// Ends the customer's current session (Slice 2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class SignOutOperationsCustomerUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public SignOutOperationsCustomerUseCase(
        OperationsCustomerAccounts accounts,
        IOperationsUnitOfWork unitOfWork)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<SignOutOperationsCustomerResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SignOutOperationsCustomerRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is { } signedIn)
        {
            signedIn.Session.End(OperationsSessionEndReason.SignedOut, _accounts.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return SignOutOperationsCustomerResponse.Success();
    }
}
