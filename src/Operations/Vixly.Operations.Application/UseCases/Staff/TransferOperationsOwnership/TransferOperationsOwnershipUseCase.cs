using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Staff;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.UseCases.Staff.TransferOperationsOwnership;

// =========================================================
// FILE: TransferOperationsOwnershipUseCase.cs
//
// PURPOSE:
// The Owner hands ownership to an active Administrator with a second factor (Slice 1D).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Owner only, with step-up
// - There is always exactly one Owner: if the second save fails, the former Owner is an Administrator and Vixly support restores ownership
// =========================================================
public sealed class TransferOperationsOwnershipUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsUserRepository _users;

    private readonly IOperationsRoleRepository _roles;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public TransferOperationsOwnershipUseCase(
        OperationsAccess access,
        IOperationsUserRepository users,
        IOperationsRoleRepository roles,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<TransferOperationsOwnershipResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        TransferOperationsOwnershipRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        OperationsAccessView? access = await _access.LoadAsync(context, cancellationToken);

        if (access is not { IsOwner: true })
        {
            return TransferOperationsOwnershipResponse.Failure(OperationsAccessErrors.OwnerOnly);
        }

        if (!await _access.IsFreshAsync(context, cancellationToken))
        {
            return TransferOperationsOwnershipResponse.Failure(OperationsAccessErrors.StepUpRequired);
        }

        OperationsUser? next = await _users.GetAsync(request.UserId, cancellationToken);

        if (next is null)
        {
            return TransferOperationsOwnershipResponse.Failure(OperationsCommonErrors.NotFound);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsUser owner = access.User;

        try
        {
            owner.TransferOwnershipTo(next, utcNow);
        }
        catch (DomainException exception)
        {
            return TransferOperationsOwnershipResponse.Failure(exception.Error);
        }

        // The former Owner keeps the Administrator role; the new Owner's Administrator grant ends (the Owner holds everything).
        OperationsRole administrator = await _access.AdministratorRoleAsync(context.RequiredTenantId, cancellationToken);
        _roles.Add(OperationsRoleGrant.Grant(context.RequiredTenantId, owner.Id, administrator.Id, OperationsRoleScope.Tenant, null, owner.Id, utcNow));

        foreach (OperationsRoleGrant grant in (await _roles.ListGrantsAsync(next.Id, cancellationToken)).Where(grant => grant.IsActive && grant.RoleId == administrator.Id))
        {
            grant.Revoke(owner.Id, utcNow);
        }

        _audit.Record(owner.Id, "ops.staff.ownership_transferred", nameof(OperationsUser), next.Id);

        // One Owner per tenant is a unique index, so the old Owner steps down first; both saves commit together.
        return await _unitOfWork.SaveInTwoStepsAsync(() => next.BecomeOwner(utcNow), cancellationToken)
            ? TransferOperationsOwnershipResponse.Success()
            : TransferOperationsOwnershipResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
