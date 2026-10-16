using Vixly.Platform.Application.Collections;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Collections;
using Vixly.Platform.Domain.Collections;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Collections.GetPlatformCollectionsSettings;

// =========================================================
// FILE: GetPlatformCollectionsSettingsUseCase.cs
//
// PURPOSE:
// Reads the collections settings (D-C12).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetPlatformCollectionsSettingsUseCase
{
    private readonly PlatformCollectionsAdministration _collections;

    public GetPlatformCollectionsSettingsUseCase(
        PlatformCollectionsAdministration collections)
    {
        _collections = collections ?? throw new ArgumentNullException(nameof(collections));
    }

    public async Task<GetPlatformCollectionsSettingsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformCollectionsSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _collections.AuthorizeAsync(context, PlatformPermissions.CollectionsView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return GetPlatformCollectionsSettingsResponse.Failure(refused);
        }

        PlatformCollectionsSettings settings = await _collections.SettingsAsync(cancellationToken);

        return GetPlatformCollectionsSettingsResponse.Success(new PlatformCollectionsSettingsItem(
            settings.Enabled, settings.Reminder1Day, settings.Reminder2Day, settings.FinalWarningDay, settings.BlockDay, settings.RetryDayList,
            settings.MinimumNoticeDays, settings.BlockedReadOnlyDays, settings.ExtensionMaxDays, settings.ExtensionsPerCase,
            settings.PendingPaymentReadOnlyDays, settings.LongBlockDays, settings.LastModifiedUtc,
            await _collections.HoldsAsync(context, PlatformPermissions.CollectionsConfigure, cancellationToken)));
    }
}
