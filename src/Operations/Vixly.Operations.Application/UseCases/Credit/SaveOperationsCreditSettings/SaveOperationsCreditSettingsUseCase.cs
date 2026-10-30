using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Credit.SaveOperationsCreditSettings;

// =========================================================
// FILE: SaveOperationsCreditSettingsUseCase.cs
//
// PURPOSE:
// Save the company's credit settings (Slice 5F step 2b, K-2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.settings.edit
// =========================================================
public sealed class SaveOperationsCreditSettingsUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsCreditSettingsUseCase(
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

    public async Task<SaveOperationsCreditSettingsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsCreditSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsEdit, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsCreditSettingsResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        if (settings is null)
        {
            settings = OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
            _settings.Add(settings);
        }

        object before = new { days = settings.CreditTermsDays, overLimit = settings.CreditOverLimit.ToString(), statements = settings.CreditStatements };

        try
        {
            settings.ChangeCredit(request.TermsDays, request.OverLimit, request.Statements, utcNow);
        }
        catch (DomainException exception)
        {
            return SaveOperationsCreditSettingsResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, "ops.credit.settings_saved", nameof(OperationsTenantSettings), settings.Id, before: before,
            after: new { days = settings.CreditTermsDays, overLimit = settings.CreditOverLimit.ToString(), statements = settings.CreditStatements });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SaveOperationsCreditSettingsResponse.Success()
            : SaveOperationsCreditSettingsResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
