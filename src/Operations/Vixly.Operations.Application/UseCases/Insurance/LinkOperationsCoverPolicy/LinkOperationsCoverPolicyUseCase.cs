using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Insurance;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Insurance;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Insurance.LinkOperationsCoverPolicy;

// =========================================================
// FILE: LinkOperationsCoverPolicyUseCase.cs
//
// PURPOSE:
// Names the company policy a cover sold to customers sits under, or none (self-insured) (Slice 5B, design section 4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.insurance.manage
// - Certificates already issued keep the policy they named
// =========================================================
public sealed class LinkOperationsCoverPolicyUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsInsuranceRepository _insurance;

    private readonly IOperationsChargeRepository _charges;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public LinkOperationsCoverPolicyUseCase(
        OperationsAccess access,
        IOperationsInsuranceRepository insurance,
        IOperationsChargeRepository charges,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _insurance = insurance ?? throw new ArgumentNullException(nameof(insurance));
        _charges = charges ?? throw new ArgumentNullException(nameof(charges));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<LinkOperationsCoverPolicyResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        LinkOperationsCoverPolicyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.InsuranceManage, false, cancellationToken);

        if (refused is not null)
        {
            return LinkOperationsCoverPolicyResponse.Failure(refused);
        }

        if ((request.PlanId is { } planId ? await _charges.GetInsurancePlanAsync(planId, cancellationToken) : await _charges.FindInsurancePlanAsync(request.Mode, cancellationToken))
            is not { } plan || plan.Mode != request.Mode)
        {
            return LinkOperationsCoverPolicyResponse.Failure(OperationsInsuranceErrors.NotFound);
        }

        // A policy for customers' shipments, still held.
        if (request.PolicyId is { } policyId)
        {
            if (await _insurance.GetPolicyAsync(policyId, cancellationToken) is not { Scope: OperationsPolicyScope.CustomerShipments, IsArchived: false } policy)
            {
                return LinkOperationsCoverPolicyResponse.Failure(OperationsInsuranceErrors.PolicyScopeInvalid);
            }

            if (!policy.Covers(plan.Mode))
            {
                return LinkOperationsCoverPolicyResponse.Failure(OperationsInsuranceErrors.PolicyModeInvalid);
            }
        }

        if (plan.PolicyId == request.PolicyId)
        {
            return LinkOperationsCoverPolicyResponse.Success();
        }

        Guid? before = plan.PolicyId;
        plan.SitUnder(request.PolicyId, _timeProvider.GetUtcNow());
        _audit.Record(access!.User.Id, "ops.insurance.cover_policy_changed", nameof(OperationsInsurancePlan), plan.Id, before: new { policy = before }, after: new { policy = request.PolicyId });
        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? LinkOperationsCoverPolicyResponse.Success()
            : LinkOperationsCoverPolicyResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
