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

namespace Vixly.Operations.Application.UseCases.Customers.VerifyOperationsCustomerEmail;

// =========================================================
// FILE: VerifyOperationsCustomerEmailUseCase.cs
//
// PURPOSE:
// Confirms a customer's email from the link: the account becomes active, gets its code and is signed in (Slice 2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class VerifyOperationsCustomerEmailUseCase
{
    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsUserRepository _staff;

    private readonly OperationsCustomerAccounts _accounts;

    private readonly OperationsNumbering _numbering;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public VerifyOperationsCustomerEmailUseCase(
        IOperationsCustomerRepository customers,
        IOperationsUserRepository staff,
        OperationsCustomerAccounts accounts,
        OperationsNumbering numbering,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork)
    {
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _staff = staff ?? throw new ArgumentNullException(nameof(staff));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<VerifyOperationsCustomerEmailResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        VerifyOperationsCustomerEmailRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        OperationsCustomerToken? token = string.IsNullOrWhiteSpace(request.Token) ? null : await _customers.FindTokenAsync(_accounts.HashToken(request.Token), cancellationToken);
        OperationsCustomer? customer = token is null ? null : await _customers.GetAsync(token.CustomerId, cancellationToken);
        DateTimeOffset utcNow = _accounts.UtcNow;

        if (token is null || customer is null || customer.Status != OperationsCustomerStatus.PendingVerification)
        {
            return VerifyOperationsCustomerEmailResponse.Failure(OperationsCustomerErrors.LinkInvalid);
        }

        // Review item 3: checked again here, for a work email that became one after the sign-up (only the mailbox's owner sees this).
        if (await _staff.IsStaffEmailAnywhereAsync(customer.Email, cancellationToken))
        {
            return VerifyOperationsCustomerEmailResponse.Failure(OperationsCustomerErrors.EmailUnavailable);
        }

        try
        {
            token.Consume(OperationsCustomerTokenPurpose.Verification, utcNow);
            customer.ConfirmEmail(await _numbering.NextCustomerCodeAsync(cancellationToken), utcNow);
        }
        catch (DomainException exception)
        {
            return VerifyOperationsCustomerEmailResponse.Failure(exception.Error);
        }

        string session = _accounts.StartSession(customer, utcNow);
        _audit.Record(null, "ops.customers.email_confirmed", nameof(OperationsCustomer), customer.Id, after: new { code = customer.Code });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? VerifyOperationsCustomerEmailResponse.Success(session, await _accounts.ProfileAsync(customer, cancellationToken))
            : VerifyOperationsCustomerEmailResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
