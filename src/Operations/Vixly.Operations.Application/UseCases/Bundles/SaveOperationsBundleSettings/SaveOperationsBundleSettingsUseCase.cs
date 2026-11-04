using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.SaveOperationsBundleSettings;

// =========================================================
// FILE: SaveOperationsBundleSettingsUseCase.cs
//
// PURPOSE:
// Save the company's bundle settings (Slice 6A, D-1).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.settings.edit
// =========================================================
public sealed class SaveOperationsBundleSettingsUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsBundleSettingsUseCase(
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

    public async Task<SaveOperationsBundleSettingsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsBundleSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsEdit, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsBundleSettingsResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        if (settings is null)
        {
            settings = OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
            _settings.Add(settings);
        }

        object before = new { types = settings.BundleTypes, rule = settings.BundleTargetRule.ToString(), way = settings.ConsolidationWay.ToString() };

        try
        {
            settings.ChangeBundles(request.Types, request.TargetRule, request.Way, utcNow);
        }
        catch (DomainException exception)
        {
            return SaveOperationsBundleSettingsResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, "ops.bundles.settings_saved", nameof(OperationsTenantSettings), settings.Id, before: before,
            after: new { types = settings.BundleTypes, rule = settings.BundleTargetRule.ToString(), way = settings.ConsolidationWay.ToString() });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SaveOperationsBundleSettingsResponse.Success()
            : SaveOperationsBundleSettingsResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
