using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.UseCases.MyAccount.GetMyPlatformAccess;

// =========================================================
// FILE: GetMyPlatformAccessUseCase.cs
//
// PURPOSE:
// Tells the console which permissions the signed-in user holds
// right now, so it shows only what they can use.
//
// RESPONSIBILITIES:
// - Read the user's access fresh (account state, authority,
//   roles in force)
// - A SuperUser holds every permission; an inactive account
//   holds none
//
// IMPORTANT NOTES:
// - Menu visibility only. Every use case still checks its own
//   permission through the gate (S-1)
// =========================================================
public sealed class GetMyPlatformAccessUseCase
{
    private readonly IPlatformPermissionReader _permissions;

    private readonly TimeProvider _timeProvider;

    public GetMyPlatformAccessUseCase(IPlatformPermissionReader permissions, TimeProvider timeProvider)
    {
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetMyPlatformAccessResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetMyPlatformAccessRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformPermissionSnapshot? snapshot = await _permissions.ReadAsync(
            context.PlatformUserId,
            _timeProvider.GetUtcNow(),
            cancellationToken);

        if (snapshot is null || !snapshot.IsAccountActive)
        {
            return GetMyPlatformAccessResponse.Success(Array.Empty<string>(), false);
        }

        bool isSuperUser = snapshot.AuthorityLevel == PlatformUserAuthorityLevel.SuperUser;
        IReadOnlyCollection<string> codes = isSuperUser
            ? PlatformPermissions.All
            : snapshot.PermissionCodes.Where(PlatformPermissions.IsKnown).ToHashSet(StringComparer.Ordinal);

        return GetMyPlatformAccessResponse.Success(codes, isSuperUser);
    }
}
