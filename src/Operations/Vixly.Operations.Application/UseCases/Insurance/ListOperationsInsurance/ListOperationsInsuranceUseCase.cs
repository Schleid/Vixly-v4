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

namespace Vixly.Operations.Application.UseCases.Insurance.ListOperationsInsurance;

// =========================================================
// FILE: ListOperationsInsuranceUseCase.cs
//
// PURPOSE:
// The insurance page: the company's policies, the cover it sells per mode and the policy each sits under, its liability terms (Slice 5B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.insurance.view; changes need ops.insurance.manage
// =========================================================
public sealed class ListOperationsInsuranceUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsInsuranceRepository _insurance;

    private readonly IOperationsChargeRepository _charges;

    private readonly OperationsLiability _liability;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public ListOperationsInsuranceUseCase(
        OperationsAccess access,
        IOperationsInsuranceRepository insurance,
        IOperationsChargeRepository charges,
        OperationsLiability liability,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _insurance = insurance ?? throw new ArgumentNullException(nameof(insurance));
        _charges = charges ?? throw new ArgumentNullException(nameof(charges));
        _liability = liability ?? throw new ArgumentNullException(nameof(liability));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ListOperationsInsuranceResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsInsuranceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.InsuranceView, false, cancellationToken);

        if (refused is not null)
        {
            return ListOperationsInsuranceResponse.Failure(refused);
        }

        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);
        DateOnly today = Vixly.Operations.Domain.Locations.OperationsTimeZones.Today(_timeProvider.GetUtcNow(), settings?.TimeZone);
        return ListOperationsInsuranceResponse.Success(await _insurance.ListPoliciesAsync(cancellationToken), await _charges.ListInsurancePlansAsync(cancellationToken),
            await _liability.ListAsync(cancellationToken), today, access!.Has(OperationsPermissions.InsuranceManage) || access.IsAdministrator, settings);
    }
}
