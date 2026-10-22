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

namespace Vixly.Operations.Application.UseCases.Customers.ResetOperationsCustomerPassword;

// =========================================================
// FILE: ResetOperationsCustomerPasswordUseCase.cs
//
// PURPOSE:
// Sets a new password from a reset or invitation link, ends every session and signs in (Slice 2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ResetOperationsCustomerPasswordUseCase
{
    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsPasswordHasher _hasher;

    private readonly OperationsNumbering _numbering;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public ResetOperationsCustomerPasswordUseCase(
        IOperationsCustomerRepository customers,
        OperationsCustomerAccounts accounts,
        IOperationsPasswordHasher hasher,
        OperationsNumbering numbering,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork)
    {
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ResetOperationsCustomerPasswordResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ResetOperationsCustomerPasswordRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        OperationsCustomerToken? token = string.IsNullOrWhiteSpace(request.Token) ? null : await _customers.FindTokenAsync(_accounts.HashToken(request.Token), cancellationToken);
        OperationsCustomer? customer = token is null ? null : await _customers.GetAsync(token.CustomerId, cancellationToken);
        DateTimeOffset utcNow = _accounts.UtcNow;

        if (token is null || customer is null || token.Purpose == OperationsCustomerTokenPurpose.Verification
            || customer.Status is OperationsCustomerStatus.Suspended or OperationsCustomerStatus.Closed)
        {
            return ResetOperationsCustomerPasswordResponse.Failure(OperationsCustomerErrors.LinkInvalid);
        }

        if (OperationsPasswordPolicy.Check(request.Password, customer.Email.Value) is { } weak)
        {
            return ResetOperationsCustomerPasswordResponse.Failure(weak);
        }

        try
        {
            token.Consume(token.Purpose, utcNow);
            customer.SetPassword(_hasher.Hash(request.Password), utcNow);

            // The link proves the email: a customer who never confirmed is confirmed now.
            if (customer.Status == OperationsCustomerStatus.PendingVerification)
            {
                customer.ConfirmEmail(await _numbering.NextCustomerCodeAsync(cancellationToken), utcNow);
            }
        }
        catch (DomainException exception)
        {
            return ResetOperationsCustomerPasswordResponse.Failure(exception.Error);
        }

        await _accounts.EndSessionsAsync(customer.Id, OperationsSessionEndReason.PasswordChanged, null, cancellationToken);
        await _accounts.CancelPasswordLinksAsync(customer.Id, cancellationToken);
        string session = _accounts.StartSession(customer, utcNow);
        _audit.Record(null, "ops.customers.password_reset", nameof(OperationsCustomer), customer.Id);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ResetOperationsCustomerPasswordResponse.Success(session, await _accounts.ProfileAsync(customer, cancellationToken))
            : ResetOperationsCustomerPasswordResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
