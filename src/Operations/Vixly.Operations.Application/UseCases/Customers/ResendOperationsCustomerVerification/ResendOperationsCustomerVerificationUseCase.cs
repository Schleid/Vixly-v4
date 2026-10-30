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

namespace Vixly.Operations.Application.UseCases.Customers.ResendOperationsCustomerVerification;

// =========================================================
// FILE: ResendOperationsCustomerVerificationUseCase.cs
//
// PURPOSE:
// Sends a new confirmation link; the same answer for unknown emails (Slice 2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ResendOperationsCustomerVerificationUseCase
{
    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsCustomerAccounts _accounts;

    public ResendOperationsCustomerVerificationUseCase(
        IOperationsCustomerRepository customers,
        OperationsCustomerAccounts accounts)
    {
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
    }

    public async Task<ResendOperationsCustomerVerificationResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ResendOperationsCustomerVerificationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (EmailAddress.TryCreate(request.Email, out EmailAddress? email)
            && await _customers.FindByEmailAsync(email!, cancellationToken) is { Status: OperationsCustomerStatus.PendingVerification } customer)
        {
            await _accounts.SendLinkAsync(customer, OperationsCustomerTokenPurpose.Verification, request.PortalSlug, cancellationToken);
        }

        return ResendOperationsCustomerVerificationResponse.Success();
    }
}
