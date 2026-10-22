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

namespace Vixly.Operations.Application.UseCases.Customers.SignInOperationsCustomer;

// =========================================================
// FILE: SignInOperationsCustomerUseCase.cs
//
// PURPOSE:
// A customer signs in with email and password (Slice 2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class SignInOperationsCustomerUseCase
{
    // A BCrypt hash (cost 12) of a random value nobody knows, checked when there is no customer to check.
    private const string TimingEqualiser = "$2a$12$FC8a3NViTOEkLGndH5yps.gTnwWuns1YztQZwyA8N8qHH2rQTOMAm";

    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsPasswordHasher _hasher;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public SignInOperationsCustomerUseCase(
        IOperationsCustomerRepository customers,
        OperationsCustomerAccounts accounts,
        IOperationsPasswordHasher hasher,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork)
    {
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<SignInOperationsCustomerResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SignInOperationsCustomerRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        DateTimeOffset utcNow = _accounts.UtcNow;

        // An empty or over-long password is refused before the look-up, the same way for every email (review L2).
        if (string.IsNullOrEmpty(request.Password) || request.Password.Length > OperationsPasswordPolicy.MaxLength)
        {
            _hasher.Verify("-", TimingEqualiser);
            return SignInOperationsCustomerResponse.Failure(OperationsCustomerAuthErrors.SignInFailed);
        }

        OperationsCustomer? customer = EmailAddress.TryCreate(request.Email, out EmailAddress? email) ? await _customers.FindByEmailAsync(email!, cancellationToken) : null;

        // Unknown, closed, suspended and locked customers get the same answer as a wrong password.
        if (customer?.PasswordHash is null || customer.IsLockedOut(utcNow)
            || customer.Status is OperationsCustomerStatus.Suspended or OperationsCustomerStatus.Closed)
        {
            _hasher.Verify(request.Password ?? string.Empty, TimingEqualiser);
            return SignInOperationsCustomerResponse.Failure(OperationsCustomerAuthErrors.SignInFailed);
        }

        if (_hasher.Verify(request.Password, customer.PasswordHash) is OperationsPasswordCheck.Failed)
        {
            await _customers.RecordFailedSignInAsync(customer.Id, utcNow, cancellationToken);
            _audit.Record(null, "ops.customers.sign_in_failed", nameof(OperationsCustomer), customer.Id);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return SignInOperationsCustomerResponse.Failure(OperationsCustomerAuthErrors.SignInFailed);
        }

        // The password was right: only now may the answer say that the email is not confirmed.
        if (customer.Status == OperationsCustomerStatus.PendingVerification)
        {
            return SignInOperationsCustomerResponse.Failure(OperationsCustomerAuthErrors.NotVerified);
        }

        string session = _accounts.StartSession(customer, utcNow);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SignInOperationsCustomerResponse.Success(session, await _accounts.ProfileAsync(customer, cancellationToken))
            : SignInOperationsCustomerResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
