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

namespace Vixly.Operations.Application.UseCases.Customers.SetOperationsCustomerCredit;

// =========================================================
// FILE: SetOperationsCustomerCreditUseCase.cs
//
// PURPOSE:
// Staff allow or remove credit for a customer, with a reason (Slice 2; used by the payment gate from Slice 9).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class SetOperationsCustomerCreditUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsCustomerRepository _customers;

    private readonly TimeProvider _timeProvider;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public SetOperationsCustomerCreditUseCase(
        OperationsAccess access,
        IOperationsCustomerRepository customers,
        TimeProvider timeProvider,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<SetOperationsCustomerCreditResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SetOperationsCustomerCreditRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.CustomersCredit, true, cancellationToken);

        if (refused is not null)
        {
            return SetOperationsCustomerCreditResponse.Failure(refused);
        }

        if (await _customers.GetAsync(request.CustomerId, cancellationToken) is not { } customer)
        {
            return SetOperationsCustomerCreditResponse.Failure(OperationsCommonErrors.NotFound);
        }

        try
        {
            customer.SetCredit(request.Allowed, request.Reason, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return SetOperationsCustomerCreditResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, "ops.customers.credit_changed", nameof(OperationsCustomer), customer.Id, after: new { allowed = request.Allowed }, reason: customer.CreditReason);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SetOperationsCustomerCreditResponse.Success()
            : SetOperationsCustomerCreditResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
