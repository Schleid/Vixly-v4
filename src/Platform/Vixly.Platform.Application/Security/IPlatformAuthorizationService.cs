namespace Vixly.Platform.Application.Security;

// =========================================================
// FILE: IPlatformAuthorizationService.cs
//
// PURPOSE:
// Port that answers whether a Platform user currently holds a
// permission.
//
// RESPONSIBILITIES:
// - Resolve the user's active role assignments from persistence
// - Report whether one required permission, or any of a set,
//   is held
//
// IMPORTANT NOTES:
// - Implemented in Infrastructure (Identity slice)
// - Must read current persisted assignments on every call
//   path it is used for; it must not trust caller-supplied
//   permission lists
// - An inactive, locked or suspended account holds no
//   permissions
// =========================================================
public interface IPlatformAuthorizationService
{
    Task<bool> IsAuthorizedAsync(
        PlatformAuthorizationContext context,
        string requiredPermissionKey,
        CancellationToken cancellationToken = default);

    Task<bool> IsAuthorizedForAnyAsync(
        PlatformAuthorizationContext context,
        IReadOnlyCollection<string> requiredPermissionKeys,
        CancellationToken cancellationToken = default);
}
