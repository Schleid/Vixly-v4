using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.SecuritySettings.GetPlatformSecuritySettings;

// =========================================================
// FILE: GetPlatformSecuritySettingsUseCase.cs
//
// PURPOSE:
// Shows Control's sign-in rule (Require MFA) to those who see
// the users (owner, 2026-10-22).
//
// RESPONSIBILITIES:
// - Read the rule; no row means Yes
// - Say whether the reader may change it (SuperUser only)
//
// IMPORTANT NOTES:
// - Changing it is SuperUser only: the right is
//   platform.users.change_authority, which no role carries
// =========================================================
public sealed class GetPlatformSecuritySettingsUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformPermissionReader _permissions;

    private readonly IPlatformSecuritySettingsRepository _settings;

    private readonly IPlatformSubscriptionQueries _queries;

    private readonly TimeProvider _timeProvider;

    public GetPlatformSecuritySettingsUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformPermissionReader permissions,
        IPlatformSecuritySettingsRepository settings,
        IPlatformSubscriptionQueries queries,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetPlatformSecuritySettingsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformSecuritySettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.UsersView, cancellationToken);

        PlatformSecuritySettings? settings = await _settings.GetAsync(cancellationToken);
        string? by = null;

        if (settings?.UpdatedByUserId is Guid editor)
        {
            (await _queries.StaffNamesAsync(new[] { editor }, cancellationToken)).TryGetValue(editor, out by);
        }

        PlatformPermissionSnapshot? actor = await _permissions.ReadAsync(context.PlatformUserId, _timeProvider.GetUtcNow(), cancellationToken);
        bool canEdit = actor is not null && actor.IsAccountActive && PlatformAccess.Holds(actor, PlatformPermissions.UsersChangeAuthority);

        bool canEditIdle = actor is not null && actor.IsAccountActive && PlatformAccess.Holds(actor, PlatformPermissions.UsersEndSessions);
        PlatformSecuritySettings shown = settings ?? PlatformSecuritySettings.Default(_timeProvider.GetUtcNow());

        return GetPlatformSecuritySettingsResponse.Success(PlatformSecuritySettings.Requires(settings), by, settings?.LastModifiedUtc, canEdit,
            shown.IdleMinutes, shown.IdleWarnMinutes, canEditIdle);
    }
}
