using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Items;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Items;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Items.ChangeOperationsItemRule;

// =========================================================
// FILE: ChangeOperationsItemRuleUseCase.cs
//
// PURPOSE:
// Switches on or off the rule that items need a customs code and a country of origin before bundling (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Switched on, it applies to parcels created from that moment; older parcels are never stopped
// =========================================================
public sealed class ChangeOperationsItemRuleUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ChangeOperationsItemRuleUseCase(OperationsAccess access, IOperationsSettingsRepository settings, OperationsAuditTrail audit, IOperationsUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ChangeOperationsItemRuleResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChangeOperationsItemRuleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.ServicesManage, false, cancellationToken);

        if (refused is not null)
        {
            return ChangeOperationsItemRuleResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        Vixly.Operations.Domain.Settings.OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        if (settings is null)
        {
            settings = Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
            _settings.Add(settings);
        }

        settings.RequireItems(request.Required, utcNow);
        _audit.Record(access!.User.Id, request.Required ? "ops.items.required_on" : "ops.items.required_off", "OperationsTenantSettings", null);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ChangeOperationsItemRuleResponse.Success()
            : ChangeOperationsItemRuleResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
