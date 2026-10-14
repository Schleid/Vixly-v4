using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Security;

// =========================================================
// FILE: PlatformAuthorizationService.cs
//
// PURPOSE:
// Decides whether a Platform user holds a permission, from
// persistence, at the moment of the check.
//
// RESPONSIBILITIES:
// - Deny unknown users and accounts that are not active
// - Allow a SuperUser everything
// - Otherwise allow when an active role in force grants the
//   code
//
// IMPORTANT NOTES:
// - The authority level comes from persistence, not from the
//   context, so a demotion takes effect at once
// - Unknown permission codes are denied: a typo can never
//   open access
// =========================================================
public sealed class PlatformAuthorizationService : IPlatformAuthorizationService
{
    private readonly IPlatformPermissionReader _reader;

    private readonly TimeProvider _timeProvider;

    public PlatformAuthorizationService(IPlatformPermissionReader reader, TimeProvider timeProvider)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public Task<bool> IsAuthorizedAsync(
        PlatformAuthorizationContext context,
        string requiredPermissionKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requiredPermissionKey);

        return IsAuthorizedForAnyAsync(context, new[] { requiredPermissionKey }, cancellationToken);
    }

    public async Task<bool> IsAuthorizedForAnyAsync(
        PlatformAuthorizationContext context,
        IReadOnlyCollection<string> requiredPermissionKeys,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requiredPermissionKeys);

        string[] known = requiredPermissionKeys.Where(PlatformPermissions.IsKnown).ToArray();

        if (known.Length == 0)
        {
            return false;
        }

        PlatformPermissionSnapshot? snapshot = await _reader.ReadAsync(
            context.PlatformUserId,
            _timeProvider.GetUtcNow(),
            cancellationToken);

        if (snapshot is null || !snapshot.IsAccountActive)
        {
            return false;
        }

        if (snapshot.AuthorityLevel == PlatformUserAuthorityLevel.SuperUser)
        {
            return true;
        }

        return known.Any(snapshot.PermissionCodes.Contains);
    }
}
