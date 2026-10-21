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

namespace Vixly.Operations.Application.UseCases.Customers.SignUpOperationsCustomer;

// =========================================================
// FILE: SignUpOperationsCustomerUseCase.cs
//
// PURPOSE:
// A customer signs up on the tenant portal (Slice 2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class SignUpOperationsCustomerUseCase
{
    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsPasswordHasher _hasher;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public SignUpOperationsCustomerUseCase(
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

    public async Task<SignUpOperationsCustomerResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SignUpOperationsCustomerRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (!EmailAddress.TryCreate(request.Email, out EmailAddress? email))
        {
            return SignUpOperationsCustomerResponse.Failure(OperationsCustomerAuthErrors.EmailInvalid);
        }

        if (!LanguageCode.TryCreate(request.Language, out LanguageCode? language) || language!.Value is not ("en" or "fr" or "es"))
        {
            return SignUpOperationsCustomerResponse.Failure(OperationsCustomerAuthErrors.LanguageInvalid);
        }

        if (OperationsPasswordPolicy.Check(request.Password, email!.Value) is { } weak)
        {
            return SignUpOperationsCustomerResponse.Failure(weak);
        }

        // Every field is checked before the look-up, so a known email cannot be told from its answer (review M1).
        try
        {
            OperationsCustomer.CheckDetails(request.FirstName, request.LastName, request.CompanyName, request.Phone);
        }
        catch (DomainException exception)
        {
            return SignUpOperationsCustomerResponse.Failure(exception.Error);
        }

        // Hashed first, so a known email takes as long as a new one.
        string passwordHash = _hasher.Hash(request.Password);

        // The same answer whether or not the email is known: an existing customer gets an email instead (no enumeration).
        if (await _customers.FindByEmailAsync(email, cancellationToken) is { } existing)
        {
            if (existing.Status == OperationsCustomerStatus.PendingVerification)
            {
                await _accounts.SendLinkAsync(existing, OperationsCustomerTokenPurpose.Verification, request.PortalSlug, cancellationToken);
            }
            else if (existing.Status != OperationsCustomerStatus.Closed)
            {
                await _accounts.SendAsync(existing, OperationsCustomerMailKind.AccountExists, _accounts.SignInLink(request.PortalSlug), cancellationToken);
            }

            return SignUpOperationsCustomerResponse.Success();
        }

        OperationsCustomer customer;

        try
        {
            customer = OperationsCustomer.SignUp(context.RequiredTenantId, request.FirstName, request.LastName, request.CompanyName, email, request.Phone, language, passwordHash, _accounts.UtcNow);
        }
        catch (DomainException exception)
        {
            return SignUpOperationsCustomerResponse.Failure(exception.Error);
        }

        _customers.Add(customer);
        _audit.Record(null, "ops.customers.signed_up", nameof(OperationsCustomer), customer.Id);

        return await _accounts.SendLinkAsync(customer, OperationsCustomerTokenPurpose.Verification, request.PortalSlug, cancellationToken)
            ? SignUpOperationsCustomerResponse.Success()
            : SignUpOperationsCustomerResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
