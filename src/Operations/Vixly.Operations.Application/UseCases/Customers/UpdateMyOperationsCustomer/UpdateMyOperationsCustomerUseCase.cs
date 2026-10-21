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

namespace Vixly.Operations.Application.UseCases.Customers.UpdateMyOperationsCustomer;

// =========================================================
// FILE: UpdateMyOperationsCustomerUseCase.cs
//
// PURPOSE:
// The signed-in customer changes their name, company, phone or language (Slice 2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class UpdateMyOperationsCustomerUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public UpdateMyOperationsCustomerUseCase(
        OperationsCustomerAccounts accounts,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<UpdateMyOperationsCustomerResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        UpdateMyOperationsCustomerRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return UpdateMyOperationsCustomerResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        if (!LanguageCode.TryCreate(request.Language, out LanguageCode? language) || language!.Value is not ("en" or "fr" or "es"))
        {
            return UpdateMyOperationsCustomerResponse.Failure(OperationsCustomerAuthErrors.LanguageInvalid);
        }

        try
        {
            signedIn.Customer.ChangeProfile(request.FirstName, request.LastName, request.CompanyName, request.Phone, language, _accounts.UtcNow);
        }
        catch (DomainException exception)
        {
            return UpdateMyOperationsCustomerResponse.Failure(exception.Error);
        }

        _audit.Record(null, "ops.customers.profile_changed", nameof(OperationsCustomer), signedIn.Customer.Id);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? UpdateMyOperationsCustomerResponse.Success(await _accounts.ProfileAsync(signedIn.Customer, cancellationToken))
            : UpdateMyOperationsCustomerResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
