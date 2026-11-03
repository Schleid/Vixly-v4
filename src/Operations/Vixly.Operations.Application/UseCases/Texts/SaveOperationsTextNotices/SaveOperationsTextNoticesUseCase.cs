using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Texts;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Texts;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Texts.SaveOperationsTextNotices;

// =========================================================
// FILE: SaveOperationsTextNoticesUseCase.cs
//
// PURPOSE:
// The notices the company sends by text, and its WhatsApp templates (Slice 9).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.settings.edit
// =========================================================
public sealed class SaveOperationsTextNoticesUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsTextDesk _texts;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsTextNoticesUseCase(
        OperationsAccess access,
        OperationsTextDesk texts,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _texts = texts ?? throw new ArgumentNullException(nameof(texts));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsTextNoticesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsTextNoticesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsEdit, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsTextNoticesResponse.Failure(refused);
        }

        if (!await _texts.InPlanAsync(context.RequiredTenantId, cancellationToken))
        {
            return SaveOperationsTextNoticesResponse.Failure(OperationsTextErrors.FeatureOff);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        if (settings is null)
        {
            settings = OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
            _settings.Add(settings);
        }

        try
        {
            settings.ChangeTextNotices(request.Notices, utcNow);

            if (await _texts.Texts.GetProviderAsync(cancellationToken) is { } provider)
            {
                provider.ChangeTemplates(request.Templates, utcNow);
            }
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            return SaveOperationsTextNoticesResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, "ops.settings.text_notices_changed", nameof(OperationsTenantSettings), settings.Id, after: new { notices = settings.TextNotices.ToString() });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SaveOperationsTextNoticesResponse.Success()
            : SaveOperationsTextNoticesResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
