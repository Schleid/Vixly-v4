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

namespace Vixly.Operations.Application.UseCases.Customers.ChangeOperationsCustomerStatus;

// =========================================================
// FILE: ChangeOperationsCustomerStatusUseCase.cs
//
// PURPOSE:
// Staff suspend, reactivate or close a customer with a reason; the customer's sessions end (Slice 2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ChangeOperationsCustomerStatusUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsCustomerAccounts _accounts;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public ChangeOperationsCustomerStatusUseCase(
        OperationsAccess access,
        IOperationsCustomerRepository customers,
        OperationsCustomerAccounts accounts,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ChangeOperationsCustomerStatusResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChangeOperationsCustomerStatusRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.CustomersManage, false, cancellationToken);

        if (refused is not null)
        {
            return ChangeOperationsCustomerStatusResponse.Failure(refused);
        }

        if (await _customers.GetAsync(request.CustomerId, cancellationToken) is not { } customer)
        {
            return ChangeOperationsCustomerStatusResponse.Failure(OperationsCommonErrors.NotFound);
        }

        OperationsCustomerStatus before = customer.Status;

        try
        {
            customer.ChangeStatus(request.Status, request.Reason, _accounts.UtcNow);
        }
        catch (DomainException exception)
        {
            return ChangeOperationsCustomerStatusResponse.Failure(exception.Error);
        }

        if (customer.Status != OperationsCustomerStatus.Active)
        {
            await _accounts.EndSessionsAsync(customer.Id, OperationsSessionEndReason.AccountRestricted, null, cancellationToken);
        }

        _audit.Record(access!.User.Id, "ops.customers.status_changed", nameof(OperationsCustomer), customer.Id,
            before: new { status = before.ToString() }, after: new { status = customer.Status.ToString() }, reason: customer.StatusReason);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ChangeOperationsCustomerStatusResponse.Success()
            : ChangeOperationsCustomerStatusResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
