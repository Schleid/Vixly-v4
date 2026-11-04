using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Events;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Events.GetOperationsEventSettings;

// =========================================================
// FILE: GetOperationsEventSettingsUseCase.cs
//
// PURPOSE:
// Settings → Events (Slice 6C, E-3 and E-5): the emails, the standard causes and the company's own.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.settings.view; changes need ops.settings.edit
// =========================================================
public sealed class GetOperationsEventSettingsUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsEventRepository _events;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public GetOperationsEventSettingsUseCase(
        OperationsAccess access,
        IOperationsEventRepository events,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsEventSettingsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsEventSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsView, false, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsEventSettingsResponse.Failure(refused);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        return GetOperationsEventSettingsResponse.Success(settings.EventEmails, settings.DisabledEventCauseList(),
            (await _events.ListReasonsAsync(cancellationToken)).OrderBy(reason => reason.Kind).ThenBy(reason => reason.TextEn, StringComparer.CurrentCultureIgnoreCase).ToList(),
            access!.Has(OperationsPermissions.SettingsEdit), settings.PickupEmails);
    }
}
