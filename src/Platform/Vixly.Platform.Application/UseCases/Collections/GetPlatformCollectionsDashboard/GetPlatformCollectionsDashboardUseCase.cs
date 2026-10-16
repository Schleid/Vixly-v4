using Vixly.Platform.Application.Collections;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Collections;
using Vixly.Platform.Domain.Collections;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Collections.GetPlatformCollectionsDashboard;

// =========================================================
// FILE: GetPlatformCollectionsDashboardUseCase.cs
//
// PURPOSE:
// Reads the collections dashboard (D-C13).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetPlatformCollectionsDashboardUseCase
{
    private readonly PlatformCollectionsAdministration _collections;

    public GetPlatformCollectionsDashboardUseCase(
        PlatformCollectionsAdministration collections)
    {
        _collections = collections ?? throw new ArgumentNullException(nameof(collections));
    }

    public async Task<GetPlatformCollectionsDashboardResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformCollectionsDashboardRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _collections.AuthorizeAsync(context, PlatformPermissions.CollectionsView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return GetPlatformCollectionsDashboardResponse.Failure(refused);
        }

        PlatformCollectionsSettings settings = await _collections.SettingsAsync(cancellationToken);

        return GetPlatformCollectionsDashboardResponse.Success(await _collections.Queries.GetDashboardAsync(_collections.UtcNow, settings.LongBlockDays, cancellationToken));
    }
}
