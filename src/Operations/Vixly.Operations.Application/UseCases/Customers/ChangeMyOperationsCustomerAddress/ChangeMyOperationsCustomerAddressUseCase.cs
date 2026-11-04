using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Customers.ChangeMyOperationsCustomerAddress;

// =========================================================
// FILE: ChangeMyOperationsCustomerAddressUseCase.cs
//
// PURPOSE:
// The signed-in customer gives or changes their home address (owner, 2026-10-26).
//
// RESPONSIBILITIES:
// - Check the address and keep it on the customer
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The portal asks for it at the first sign-in while the customer has none
// - Audited without the address itself
// =========================================================
public sealed class ChangeMyOperationsCustomerAddressUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public ChangeMyOperationsCustomerAddressUseCase(
        OperationsCustomerAccounts accounts,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ChangeMyOperationsCustomerAddressResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChangeMyOperationsCustomerAddressRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return ChangeMyOperationsCustomerAddressResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        try
        {
            OperationsPostalAddress address = OperationsPostalAddress.Create(request.Line1, request.Line2, request.City, request.Region, request.PostalCode, request.CountryCode);
            signedIn.Customer.ChangeHomeAddress(address, _accounts.UtcNow);
        }
        catch (DomainException exception)
        {
            return ChangeMyOperationsCustomerAddressResponse.Failure(exception.Error);
        }

        _audit.Record(null, "ops.customers.address_changed", nameof(OperationsCustomer), signedIn.Customer.Id);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ChangeMyOperationsCustomerAddressResponse.Success(await _accounts.ProfileAsync(signedIn.Customer, cancellationToken))
            : ChangeMyOperationsCustomerAddressResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
