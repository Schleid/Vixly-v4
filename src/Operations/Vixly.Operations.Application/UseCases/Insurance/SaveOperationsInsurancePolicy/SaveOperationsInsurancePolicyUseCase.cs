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

namespace Vixly.Operations.Application.UseCases.Insurance.SaveOperationsInsurancePolicy;

// =========================================================
// FILE: SaveOperationsInsurancePolicyUseCase.cs
//
// PURPOSE:
// Records a new insurance policy of the company, or changes one (Slice 5B, design section 3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.insurance.manage
// =========================================================
public sealed class SaveOperationsInsurancePolicyUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsInsuranceRepository _insurance;

    private readonly IOperationsChargeRepository _charges;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsInsurancePolicyUseCase(
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

    public async Task<SaveOperationsInsurancePolicyResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsInsurancePolicyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.InsuranceManage, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsInsurancePolicyResponse.Failure(refused);
        }

        ArgumentNullException.ThrowIfNull(request.Terms);
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsInsurancePolicy? policy = null;

        if (request.PolicyId is { } policyId && (policy = await _insurance.GetPolicyAsync(policyId, cancellationToken)) is null)
        {
            return SaveOperationsInsurancePolicyResponse.Failure(OperationsInsuranceErrors.NotFound);
        }

        // A policy a cover sits under stays one for customers' shipments, covering that cover's mode.
        IReadOnlyList<OperationsInsurancePlan> behind = policy is null
            ? Array.Empty<OperationsInsurancePlan>()
            : (await _charges.ListInsurancePlansAsync(cancellationToken)).Where(plan => plan.PolicyId == policy.Id).ToList();

        if (behind.Count > 0 && request.Terms.Scope != OperationsPolicyScope.CustomerShipments)
        {
            return SaveOperationsInsurancePolicyResponse.Failure(OperationsInsuranceErrors.PolicyScopeInvalid);
        }

        if (behind.Any(plan => request.Terms.Modes != OperationsPolicyModes.None && !request.Terms.Modes.HasFlag(plan.Mode switch
        {
            OperationsTransportMode.Ocean => OperationsPolicyModes.Ocean,
            OperationsTransportMode.Ground => OperationsPolicyModes.Ground,
            _ => OperationsPolicyModes.Air
        })))
        {
            return SaveOperationsInsurancePolicyResponse.Failure(OperationsInsuranceErrors.PolicyModeInvalid);
        }

        try
        {
            if (policy is null)
            {
                policy = OperationsInsurancePolicy.Create(context.RequiredTenantId, request.Terms, utcNow);
                _insurance.Add(policy);
            }
            else
            {
                policy.Change(request.Terms, utcNow);
            }
        }
        catch (DomainException exception)
        {
            return SaveOperationsInsurancePolicyResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, request.PolicyId is null ? "ops.insurance.policy_added" : "ops.insurance.policy_changed", nameof(OperationsInsurancePolicy), policy.Id, after: new
        {
            scope = policy.Scope.ToString(), kind = policy.Kind.ToString(), insurer = policy.InsurerName, number = policy.PolicyNumber, starts = policy.StartsOn, ends = policy.EndsOn,
            perShipment = policy.LimitPerShipment, deductible = policy.DeductibleAmount
        });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SaveOperationsInsurancePolicyResponse.Success(policy.Id)
            : SaveOperationsInsurancePolicyResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
