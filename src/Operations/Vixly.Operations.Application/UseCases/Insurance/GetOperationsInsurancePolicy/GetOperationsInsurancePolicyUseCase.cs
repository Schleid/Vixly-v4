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

namespace Vixly.Operations.Application.UseCases.Insurance.GetOperationsInsurancePolicy;

// =========================================================
// FILE: GetOperationsInsurancePolicyUseCase.cs
//
// PURPOSE:
// One of the company's insurance policies with its documents (Slice 5B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.insurance.view
// =========================================================
public sealed class GetOperationsInsurancePolicyUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsInsuranceRepository _insurance;

    private readonly IOperationsChargeRepository _charges;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public GetOperationsInsurancePolicyUseCase(
        OperationsAccess access,
        IOperationsInsuranceRepository insurance,
        IOperationsChargeRepository charges,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _insurance = insurance ?? throw new ArgumentNullException(nameof(insurance));
        _charges = charges ?? throw new ArgumentNullException(nameof(charges));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsInsurancePolicyResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsInsurancePolicyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.InsuranceView, false, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsInsurancePolicyResponse.Failure(refused);
        }

        if (await _insurance.GetPolicyAsync(request.PolicyId, cancellationToken) is not { } policy)
        {
            return GetOperationsInsurancePolicyResponse.Failure(OperationsInsuranceErrors.NotFound);
        }

        DateOnly today = Vixly.Operations.Domain.Locations.OperationsTimeZones.Today(_timeProvider.GetUtcNow(), (await _settings.FindAsync(cancellationToken))?.TimeZone);
        IReadOnlyList<OperationsInsurancePlan> covers = (await _charges.ListInsurancePlansAsync(cancellationToken)).Where(plan => plan.PolicyId == policy.Id).ToList();
        return GetOperationsInsurancePolicyResponse.Success(policy, covers, today, access!.Has(OperationsPermissions.InsuranceManage) || access.IsAdministrator);
    }
}
