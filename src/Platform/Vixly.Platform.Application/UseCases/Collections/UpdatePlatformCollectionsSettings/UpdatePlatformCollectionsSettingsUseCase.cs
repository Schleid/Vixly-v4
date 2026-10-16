using Vixly.Platform.Application.Collections;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Collections;
using Vixly.Platform.Domain.Collections;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Collections.UpdatePlatformCollectionsSettings;

// =========================================================
// FILE: UpdatePlatformCollectionsSettingsUseCase.cs
//
// PURPOSE:
// Changes the collections timeline, block levels and extension limits (D-C12).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class UpdatePlatformCollectionsSettingsUseCase
{
    private readonly PlatformCollectionsAdministration _collections;

    public UpdatePlatformCollectionsSettingsUseCase(
        PlatformCollectionsAdministration collections)
    {
        _collections = collections ?? throw new ArgumentNullException(nameof(collections));
    }

    public async Task<UpdatePlatformCollectionsSettingsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        UpdatePlatformCollectionsSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _collections.AuthorizeAsync(context, PlatformPermissions.CollectionsConfigure, requireStepUp: true, cancellationToken) is { } refused)
        {
            return UpdatePlatformCollectionsSettingsResponse.Failure(refused);
        }

        PlatformCollectionsSettings settings = await _collections.SettingsAsync(cancellationToken);
        object before = new { settings.Enabled, settings.Reminder1Day, settings.Reminder2Day, settings.FinalWarningDay, settings.BlockDay, settings.RetryDays, settings.MinimumNoticeDays, settings.BlockedReadOnlyDays, settings.ExtensionMaxDays, settings.ExtensionsPerCase, settings.PendingPaymentReadOnlyDays, settings.LongBlockDays };

        try
        {
            settings.Update(request.Enabled, request.Reminder1Day, request.Reminder2Day, request.FinalWarningDay, request.BlockDay, request.RetryDays ?? Array.Empty<int>(),
                request.MinimumNoticeDays, request.BlockedReadOnlyDays, request.ExtensionMaxDays, request.ExtensionsPerCase, request.PendingPaymentReadOnlyDays, request.LongBlockDays,
                context.PlatformUserId, _collections.UtcNow);
        }
        catch (DomainException exception)
        {
            return UpdatePlatformCollectionsSettingsResponse.Failure(exception.Error);
        }

        _collections.Audit(context, "platform.collections.settings_updated", PlatformPermissions.CollectionsConfigure, nameof(PlatformCollectionsSettings), settings.Id,
            new { before, after = new { settings.Enabled, settings.Reminder1Day, settings.Reminder2Day, settings.FinalWarningDay, settings.BlockDay, settings.RetryDays, settings.MinimumNoticeDays, settings.BlockedReadOnlyDays, settings.ExtensionMaxDays, settings.ExtensionsPerCase, settings.PendingPaymentReadOnlyDays, settings.LongBlockDays } });

        return await _collections.SaveAsync(cancellationToken)
            ? UpdatePlatformCollectionsSettingsResponse.Success()
            : UpdatePlatformCollectionsSettingsResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
