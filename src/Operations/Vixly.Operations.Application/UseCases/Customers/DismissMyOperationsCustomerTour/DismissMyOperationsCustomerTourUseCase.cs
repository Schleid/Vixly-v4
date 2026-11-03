using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Application.UseCases.Customers.DismissMyOperationsCustomerTour;

// =========================================================
// FILE: DismissMyOperationsCustomerTourUseCase.cs
// PURPOSE:
// Remembers Skip or Finish for the signed-in customer's account.
// RESPONSIBILITIES:
// - Verify the customer session and save an idempotent dismissal.
// IMPORTANT NOTES:
// - Tenant and customer ownership never come from posted fields.
// =========================================================
public sealed class DismissMyOperationsCustomerTourUseCase
{
    private readonly OperationsCustomerAccounts _accounts;
    private readonly IOperationsUnitOfWork _unitOfWork;

    public DismissMyOperationsCustomerTourUseCase(OperationsCustomerAccounts accounts, IOperationsUnitOfWork unitOfWork)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<DismissMyOperationsCustomerTourResponse> ExecuteAsync(OperationsAuthorizationContext context,
        DismissMyOperationsCustomerTourRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn
            || signedIn.Customer.TenantId != context.RequiredTenantId)
            return DismissMyOperationsCustomerTourResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        if (signedIn.Customer.WelcomeTourDismissedUtc is not null) return DismissMyOperationsCustomerTourResponse.Success();
        signedIn.Customer.DismissWelcomeTour(_accounts.UtcNow);
        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? DismissMyOperationsCustomerTourResponse.Success()
            : DismissMyOperationsCustomerTourResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
