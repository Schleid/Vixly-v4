using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.UseCases.Customers.ResendOperationsCustomerConfirmation;

// =========================================================
// FILE: ResendOperationsCustomerConfirmationUseCase.cs
//
// PURPOSE:
// Staff send a customer's confirmation email again, to a corrected address if it was mistyped (owner, 2026-11-03).
//
// RESPONSIBILITIES:
// - Only while the account waits for confirmation, for staff who manage customers
// - Correct the address first when staff typed a new one: never another customer's, never a work email
// - Send a fresh link (older ones stop working), at most a few times an hour, and record who sent it
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// - The customer can still ask for the link themselves from the portal's sign-in page
// =========================================================
public sealed class ResendOperationsCustomerConfirmationUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsUserRepository _staff;

    private readonly OperationsCustomerAccounts _accounts;

    private readonly OperationsAuditTrail _audit;

    public ResendOperationsCustomerConfirmationUseCase(
        OperationsAccess access,
        IOperationsCustomerRepository customers,
        IOperationsUserRepository staff,
        OperationsCustomerAccounts accounts,
        OperationsAuditTrail audit)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _staff = staff ?? throw new ArgumentNullException(nameof(staff));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
    }

    public async Task<ResendOperationsCustomerConfirmationResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ResendOperationsCustomerConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Shared.Kernel.Errors.Error? refused) = await _access.RequireAsync(context, OperationsPermissions.CustomersManage, false, cancellationToken);

        if (refused is not null)
        {
            return ResendOperationsCustomerConfirmationResponse.Failure(refused);
        }

        if (await _customers.GetAsync(request.CustomerId, cancellationToken) is not { } customer)
        {
            return ResendOperationsCustomerConfirmationResponse.Failure(OperationsCommonErrors.NotFound);
        }

        if (customer.Status != OperationsCustomerStatus.PendingVerification)
        {
            return ResendOperationsCustomerConfirmationResponse.Failure(OperationsCustomerErrors.NotPending);
        }

        DateTimeOffset utcNow = _accounts.UtcNow;

        if (await _customers.CountTokensSinceAsync(customer.Id, OperationsCustomerTokenPurpose.Verification, utcNow.AddHours(-1), cancellationToken)
            >= OperationsCustomerLimits.ConfirmationResendsPerHour)
        {
            return ResendOperationsCustomerConfirmationResponse.Failure(OperationsCustomerErrors.ConfirmationResendLimit);
        }

        string before = customer.Email.Value;

        if (!string.IsNullOrWhiteSpace(request.Email) && !string.Equals(request.Email.Trim(), before, StringComparison.OrdinalIgnoreCase))
        {
            if (!EmailAddress.TryCreate(request.Email, out EmailAddress? corrected))
            {
                return ResendOperationsCustomerConfirmationResponse.Failure(OperationsCustomerAuthErrors.EmailInvalid);
            }

            if (await _customers.FindByEmailAsync(corrected!, cancellationToken) is { } other && other.Id != customer.Id)
            {
                return ResendOperationsCustomerConfirmationResponse.Failure(OperationsCustomerAuthErrors.EmailTaken);
            }

            // Review item 3: a work email never becomes a customer account.
            if (await _staff.IsStaffEmailAnywhereAsync(corrected!, cancellationToken))
            {
                return ResendOperationsCustomerConfirmationResponse.Failure(OperationsCustomerErrors.EmailIsStaff);
            }

            try
            {
                customer.CorrectEmail(corrected!, utcNow);
            }
            catch (DomainException exception)
            {
                return ResendOperationsCustomerConfirmationResponse.Failure(exception.Error);
            }
        }

        _audit.Record(access!.User.Id, "ops.customers.confirmation_resent", nameof(OperationsCustomer), customer.Id,
            before: new { email = before }, after: new { email = customer.Email.Value });

        // Saves the correction and the audit with the new link; older links stop working.
        return await _accounts.SendLinkAsync(customer, OperationsCustomerTokenPurpose.Verification, request.PortalSlug, cancellationToken)
            ? ResendOperationsCustomerConfirmationResponse.Success()
            : ResendOperationsCustomerConfirmationResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
