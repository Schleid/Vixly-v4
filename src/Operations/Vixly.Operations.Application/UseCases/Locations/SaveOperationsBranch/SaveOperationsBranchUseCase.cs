using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Locations.SaveOperationsBranch;

// =========================================================
// FILE: SaveOperationsBranchUseCase.cs
//
// PURPOSE:
// Creates or changes a branch (Slice 1B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Codes are unique per tenant; a clash at commit reads as "code taken"
// =========================================================
public sealed class SaveOperationsBranchUseCase
{
    private readonly IOperationsLocationRepository _locations;

    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsBranchUseCase(
        IOperationsLocationRepository locations,
        OperationsOwnerCheck owner,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsBranchResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsBranchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return SaveOperationsBranchResponse.Failure(Vixly.Operations.Application.Security.OperationsAccessErrors.SignInRequired);
        }

        OperationsBranch? branch = request.BranchId is { } id ? await _locations.GetBranchAsync(id, cancellationToken) : null;

        if (request.BranchId is not null && branch is null)
        {
            return SaveOperationsBranchResponse.Failure(OperationsCommonErrors.NotFound);
        }

        // A new branch needs the permission for the whole tenant; an existing one, for that branch (scope).
        if (branch is null ? !access.HasTenantWide(Vixly.Operations.Domain.Access.OperationsPermissions.BranchesManage) : !access.CoversBranch(Vixly.Operations.Domain.Access.OperationsPermissions.BranchesManage, branch.Id))
        {
            return SaveOperationsBranchResponse.Failure(Vixly.Operations.Application.Security.OperationsAccessErrors.PermissionRequired);
        }

        if (await _locations.CodeTakenAsync(request.Code.Trim().ToUpperInvariant(), branch?.Id, cancellationToken))
        {
            return SaveOperationsBranchResponse.Failure(OperationsLocationErrors.CodeTaken);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            OperationsLocationDetails details = new(
                request.Code, request.Name, OperationsPostalAddress.Create(request.Line1, request.Line2, request.City, request.Region, request.PostalCode, request.CountryCode),
                request.Phone, request.Email, request.OpeningHours, request.ShowOnSite);

            if (branch is null)
            {
                branch = OperationsBranch.Create(context.RequiredTenantId, details, request.Side, utcNow);
                _locations.Add(branch);
            }
            else
            {
                branch.Update(details, request.Side, request.Status, utcNow);
            }
        }
        catch (DomainException exception)
        {
            return SaveOperationsBranchResponse.Failure(exception.Error);
        }

        _audit.Record(context.UserId, request.BranchId is null ? "ops.locations.branch_created" : "ops.locations.branch_changed", nameof(OperationsBranch), branch.Id,
            after: new { code = branch.Code, country = branch.Address.CountryCode, status = branch.Status.ToString(), site = branch.ShowOnSite });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SaveOperationsBranchResponse.Success(branch.Id)
            : SaveOperationsBranchResponse.Failure(OperationsLocationErrors.CodeTaken);
    }
}
