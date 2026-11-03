using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Payables;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Payables;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Payables.ChangeOperationsBillSettings;

// =========================================================
// FILE: ChangeOperationsBillSettingsUseCase.cs
//
// PURPOSE:
// Whether bills wait for someone else’s approval (Slice 8).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.settings.edit with the second check
// =========================================================
public sealed class ChangeOperationsBillSettingsUseCase
{
    private readonly OperationsPayablesDesk _desk;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ChangeOperationsBillSettingsUseCase(
        OperationsPayablesDesk desk,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ChangeOperationsBillSettingsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChangeOperationsBillSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _desk.RequireAsync(context, OperationsPermissions.SettingsEdit, true, cancellationToken);

        if (refused is not null)
        {
            return ChangeOperationsBillSettingsResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        if (settings is null)
        {
            settings = OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
            _settings.Add(settings);
        }

        bool before = settings.BillsNeedApproval;
        settings.ChangeBills(request.NeedApproval, utcNow);
        _audit.Record(access!.User.Id, "ops.settings.bills_changed", nameof(OperationsTenantSettings), settings.Id, before: new { needApproval = before },
            after: new { needApproval = settings.BillsNeedApproval });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ChangeOperationsBillSettingsResponse.Success()
            : ChangeOperationsBillSettingsResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
