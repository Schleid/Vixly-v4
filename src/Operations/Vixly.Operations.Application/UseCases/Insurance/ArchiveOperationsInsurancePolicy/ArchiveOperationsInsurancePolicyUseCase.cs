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

namespace Vixly.Operations.Application.UseCases.Insurance.ArchiveOperationsInsurancePolicy;

// =========================================================
// FILE: ArchiveOperationsInsurancePolicyUseCase.cs
//
// PURPOSE:
// Archives an insurance policy the company no longer holds, or brings it back (Slice 5B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.insurance.manage
// - Never deleted: past claims refer to it
// =========================================================
public sealed class ArchiveOperationsInsurancePolicyUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsInsuranceRepository _insurance;

    private readonly IOperationsChargeRepository _charges;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ArchiveOperationsInsurancePolicyUseCase(
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

    public async Task<ArchiveOperationsInsurancePolicyResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ArchiveOperationsInsurancePolicyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.InsuranceManage, false, cancellationToken);

        if (refused is not null)
        {
            return ArchiveOperationsInsurancePolicyResponse.Failure(refused);
        }

        if (await _insurance.GetPolicyAsync(request.PolicyId, cancellationToken) is not { } policy)
        {
            return ArchiveOperationsInsurancePolicyResponse.Failure(OperationsInsuranceErrors.NotFound);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        policy.Archive(request.Archived, utcNow);

        // An archived policy no longer stands behind a cover: those covers become the company's own until another policy is chosen.
        if (request.Archived)
        {
            foreach (OperationsInsurancePlan plan in (await _charges.ListInsurancePlansAsync(cancellationToken)).Where(plan => plan.PolicyId == policy.Id))
            {
                if (await _charges.GetInsurancePlanAsync(plan.Id, cancellationToken) is { } tracked)
                {
                    tracked.SitUnder(null, utcNow);
                }
            }
        }

        _audit.Record(access!.User.Id, request.Archived ? "ops.insurance.policy_archived" : "ops.insurance.policy_restored", nameof(OperationsInsurancePolicy), policy.Id);
        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ArchiveOperationsInsurancePolicyResponse.Success()
            : ArchiveOperationsInsurancePolicyResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
