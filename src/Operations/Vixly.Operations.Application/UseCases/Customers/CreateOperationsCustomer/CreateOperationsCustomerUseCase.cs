using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Plans;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.UseCases.Customers.CreateOperationsCustomer;

// =========================================================
// FILE: CreateOperationsCustomerUseCase.cs
//
// PURPOSE:
// Lets staff enter a customer when the company's plan has no customer portal (Vixly Lite, owner 2026-10-30).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Only without the portal; active at once with its code; email unique in the company; no password, so no sign-in
// =========================================================
public sealed class CreateOperationsCustomerUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsPlanFeatures _features;

    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsNumbering _numbering;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public CreateOperationsCustomerUseCase(OperationsAccess access, IOperationsPlanFeatures features, IOperationsCustomerRepository customers, OperationsNumbering numbering, OperationsAuditTrail audit, IOperationsUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _features = features ?? throw new ArgumentNullException(nameof(features));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CreateOperationsCustomerResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CreateOperationsCustomerRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _access.LoadAsync(context, cancellationToken) is not { } access)
        {
            return CreateOperationsCustomerResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (!access.Has(OperationsPermissions.CustomersManage))
        {
            return CreateOperationsCustomerResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        // With the portal, customers register themselves; staff never enter them (owner, 2026-10-18).
        if ((await _features.GetAsync(context.RequiredTenantId, cancellationToken)).Includes(OperationsPlanModules.CustomerPortal))
        {
            return CreateOperationsCustomerResponse.Failure(OperationsCustomerErrors.EnteredByStaffOnlyWithoutPortal);
        }

        if (!EmailAddress.TryCreate(request.Email, out EmailAddress? email))
        {
            return CreateOperationsCustomerResponse.Failure(OperationsCustomerAuthErrors.EmailInvalid);
        }

        if (!LanguageCode.TryCreate(request.Language, out LanguageCode? language) || language!.Value is not ("en" or "fr" or "es"))
        {
            return CreateOperationsCustomerResponse.Failure(OperationsCustomerAuthErrors.LanguageInvalid);
        }

        try
        {
            OperationsCustomer.CheckDetails(request.FirstName, request.LastName, request.CompanyName, request.Phone);
        }
        catch (DomainException exception)
        {
            return CreateOperationsCustomerResponse.Failure(exception.Error);
        }

        // Owner, 2026-10-31: a banned customer's email or phone never opens another account.
        if (await new OperationsBannedContacts(_customers).IsBannedAsync(email, request.Phone, cancellationToken))
        {
            return CreateOperationsCustomerResponse.Failure(OperationsCustomerErrors.BannedContact);
        }

        if (await _customers.FindByEmailAsync(email!, cancellationToken) is not null)
        {
            return CreateOperationsCustomerResponse.Failure(OperationsCustomerAuthErrors.EmailTaken);
        }

        OperationsCustomer customer = OperationsCustomer.EnteredByStaff(context.RequiredTenantId, request.FirstName, request.LastName, request.CompanyName, email!, request.Phone, language,
            await _numbering.NextCustomerCodeAsync(cancellationToken), _timeProvider.GetUtcNow());
        _customers.Add(customer);
        _audit.Record(context.UserId, "ops.customers.entered", nameof(OperationsCustomer), customer.Id);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? CreateOperationsCustomerResponse.Success(customer.Id)
            : CreateOperationsCustomerResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
