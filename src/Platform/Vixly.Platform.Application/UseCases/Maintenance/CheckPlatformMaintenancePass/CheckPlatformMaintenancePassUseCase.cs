using Vixly.Platform.Application.Maintenance;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Maintenance;

namespace Vixly.Platform.Application.UseCases.Maintenance.CheckPlatformMaintenancePass;

// =========================================================
// FILE: CheckPlatformMaintenancePassUseCase.cs
//
// PURPOSE:
// Checks a pass link Vixly IT opens to get through a running window.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Anonymous and rate-limited by Platform.Api
// - Good while the window is announced or running, for the sites it closes; a wrong link reads as not found
// =========================================================
public sealed class CheckPlatformMaintenancePassUseCase
{
    private readonly IPlatformMaintenanceRepository _store;

    private readonly IPlatformSecretGenerator _secrets;

    private readonly TimeProvider _timeProvider;

    public CheckPlatformMaintenancePassUseCase(
        IPlatformMaintenanceRepository store,
        IPlatformSecretGenerator secrets,
        TimeProvider timeProvider)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CheckPlatformMaintenancePassResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        CheckPlatformMaintenancePassRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Token) || request.Token.Length > 200 || PlatformMaintenanceSiteCodes.Parse(request.Site) is not { } site)
        {
            return CheckPlatformMaintenancePassResponse.Failure(PlatformMaintenanceErrors.NotFound);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        return await _store.FindByPassHashAsync(_secrets.HashToken(request.Token.Trim()), cancellationToken) is { } window
            && window.Closes(site)
            && window.StatusAt(utcNow) is PlatformMaintenanceStatus.Announced or PlatformMaintenanceStatus.Running
            ? CheckPlatformMaintenancePassResponse.Success(window.EndsUtc)
            : CheckPlatformMaintenancePassResponse.Failure(PlatformMaintenanceErrors.NotFound);
    }
}
