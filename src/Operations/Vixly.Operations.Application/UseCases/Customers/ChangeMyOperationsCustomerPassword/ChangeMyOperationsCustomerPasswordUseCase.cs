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

namespace Vixly.Operations.Application.UseCases.Customers.ChangeMyOperationsCustomerPassword;

// =========================================================
// FILE: ChangeMyOperationsCustomerPasswordUseCase.cs
//
// PURPOSE:
// The signed-in customer changes their password; their other sessions end (Slice 2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ChangeMyOperationsCustomerPasswordUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsPasswordHasher _hasher;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public ChangeMyOperationsCustomerPasswordUseCase(
        OperationsCustomerAccounts accounts,
        IOperationsCustomerRepository customers,
        IOperationsPasswordHasher hasher,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ChangeMyOperationsCustomerPasswordResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChangeMyOperationsCustomerPasswordRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return ChangeMyOperationsCustomerPasswordResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        OperationsCustomer customer = signedIn.Customer;

        if (customer.PasswordHash is null || _hasher.Verify(request.CurrentPassword ?? string.Empty, customer.PasswordHash) is OperationsPasswordCheck.Failed)
        {
            // Wrong current passwords count like sign-in failures; at the lockout every session ends (review L4).
            if (await _customers.RecordFailedSignInAsync(customer.Id, _accounts.UtcNow, cancellationToken))
            {
                await _accounts.EndSessionsAsync(customer.Id, OperationsSessionEndReason.AccountRestricted, null, cancellationToken);
            }

            _audit.Record(null, "ops.customers.sign_in_failed", nameof(OperationsCustomer), customer.Id);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ChangeMyOperationsCustomerPasswordResponse.Failure(OperationsCustomerAuthErrors.PasswordWrong);
        }

        if (OperationsPasswordPolicy.Check(request.NewPassword, customer.Email.Value) is { } weak)
        {
            return ChangeMyOperationsCustomerPasswordResponse.Failure(weak);
        }

        customer.SetPassword(_hasher.Hash(request.NewPassword), _accounts.UtcNow);
        await _accounts.EndSessionsAsync(customer.Id, OperationsSessionEndReason.PasswordChanged, signedIn.Session.Id, cancellationToken);
        await _accounts.CancelPasswordLinksAsync(customer.Id, cancellationToken);
        _audit.Record(null, "ops.customers.password_changed", nameof(OperationsCustomer), customer.Id);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ChangeMyOperationsCustomerPasswordResponse.Success()
            : ChangeMyOperationsCustomerPasswordResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
