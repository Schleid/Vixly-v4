using Vixly.Platform.Application.Maintenance;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Maintenance;

namespace Vixly.Platform.Application.UseCases.Maintenance.GetPlatformMaintenanceForSite;

// =========================================================
// FILE: GetPlatformMaintenanceForSiteUseCase.cs
//
// PURPOSE:
// What one site must show: windows announced or running that close it.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Anonymous: the sites ask every minute; what it returns is shown to every visitor
// - Only announced and running windows: a window scheduled further ahead stays private
// =========================================================
public sealed class GetPlatformMaintenanceForSiteUseCase
{
    private readonly IPlatformMaintenanceRepository _store;

    private readonly TimeProvider _timeProvider;

    public GetPlatformMaintenanceForSiteUseCase(
        IPlatformMaintenanceRepository store,
        TimeProvider timeProvider)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetPlatformMaintenanceForSiteResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        GetPlatformMaintenanceForSiteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (PlatformMaintenanceSiteCodes.Parse(request.Site) is not { } site)
        {
            return GetPlatformMaintenanceForSiteResponse.Failure(PlatformCommonErrors.RequestInvalid);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        IReadOnlyList<PlatformMaintenanceWindow> open = await _store.ListOpenAsync(utcNow, cancellationToken);

        return GetPlatformMaintenanceForSiteResponse.Success(open
            .Where(window => window.Closes(site) && window.StatusAt(utcNow) is PlatformMaintenanceStatus.Announced or PlatformMaintenanceStatus.Running)
            .Select(window => PlatformMaintenanceWindowItem.Of(window, utcNow))
            .ToList());
    }
}
