using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Reports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Reports;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Reports.SaveOperationsReportRules;

// =========================================================
// FILE: SaveOperationsReportRulesUseCase.cs
//
// PURPOSE:
// Saves when a dispatch or a parcel counts as on time (Slice 11; owner, 2026-10-22: company settings).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Edit settings
// =========================================================
public sealed class SaveOperationsReportRulesUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsShippingServiceRepository _services;

    private readonly Vixly.Operations.Application.Audit.OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsReportRulesUseCase(
        OperationsAccess access,
        IOperationsSettingsRepository settings,
        IOperationsShippingServiceRepository services,
        Vixly.Operations.Application.Audit.OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsReportRulesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsReportRulesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsEdit, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsReportRulesResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        try
        {
            if (settings is null)
            {
                settings = OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
                _settings.Add(settings);
            }

            settings.ChangeReportRules(request.DispatchGraceHours, utcNow);

            foreach (OperationsShippingServiceSetup service in await _services.ListAsync(cancellationToken))
            {
                if (request.ReadyTargetDays.TryGetValue(service.ServiceCode, out int? days) && days != service.ReadyTargetDays)
                {
                    service.ChangeReadyTarget(days, utcNow);
                }
            }
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            return SaveOperationsReportRulesResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, "ops.settings.report_rules_changed", nameof(OperationsTenantSettings), settings.Id,
            after: new { graceHours = request.DispatchGraceHours, targets = request.ReadyTargetDays });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            _unitOfWork.DiscardChanges();
            return SaveOperationsReportRulesResponse.Failure(Vixly.Operations.Domain.Common.OperationsCommonErrors.ConcurrencyConflict);
        }

        return SaveOperationsReportRulesResponse.Success();
    }
}
