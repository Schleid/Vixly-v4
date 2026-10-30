using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Insurance.SaveOperationsInsuranceSettings;

// =========================================================
// FILE: SaveOperationsInsuranceSettingsUseCase.cs
//
// PURPOSE:
// The company's insurance settings: whether staff may change the insurance of an open shipment (Slice 5D, D-6).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.insurance.manage
// - The company decides: Vixly does not choose for it (owner, 2026-10-20)
// =========================================================
public sealed class SaveOperationsInsuranceSettingsUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsInsuranceSettingsUseCase(
        OperationsAccess access,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsInsuranceSettingsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsInsuranceSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.InsuranceManage, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsInsuranceSettingsResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        if (settings is null)
        {
            settings = OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
            _settings.Add(settings);
        }

        bool before = settings.StaffMayChangeInsurance;
        settings.ChangeInsuranceAtCounter(request.StaffMayChangeInsurance, utcNow);
        _audit.Record(access!.User.Id, "ops.insurance.settings_changed", nameof(OperationsTenantSettings), settings.Id, before: new { staffMayChange = before },
            after: new { staffMayChange = request.StaffMayChangeInsurance });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SaveOperationsInsuranceSettingsResponse.Success()
            : SaveOperationsInsuranceSettingsResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
