namespace Vixly.Platform.Application.Security;

// =========================================================
// FILE: PlatformAuthorizationGate.cs
//
// PURPOSE:
// Default gate: delegates the decision to the authorization
// service and throws when access is denied.
//
// RESPONSIBILITIES:
// - Validate gate inputs
// - Deny by default
//
// IMPORTANT NOTES:
// - The denial carries only the access_denied code. The missing
//   permission key is not included, so a caller cannot probe
//   the permission model
// - An empty permission set denies; it never allows
// =========================================================
public sealed class PlatformAuthorizationGate : IPlatformAuthorizationGate
{
    // Operator-facing log text only. The host returns the code
    // PlatformSecurityErrors.AccessDenied; users never see this text.
    private const string DeniedMessage = "platform.security.access_denied";

    private readonly IPlatformAuthorizationService _authorizationService;

    public PlatformAuthorizationGate(IPlatformAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService
            ?? throw new ArgumentNullException(nameof(authorizationService));
    }

    public async Task AuthorizeAsync(
        PlatformAuthorizationContext context,
        string requiredPermissionKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(requiredPermissionKey);

        bool isAuthorized = await _authorizationService.IsAuthorizedAsync(
            context,
            requiredPermissionKey,
            cancellationToken);

        if (!isAuthorized)
        {
            throw new UnauthorizedAccessException(DeniedMessage);
        }
    }

    public async Task AuthorizeAnyAsync(
        PlatformAuthorizationContext context,
        IReadOnlyCollection<string> requiredPermissionKeys,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requiredPermissionKeys);

        if (requiredPermissionKeys.Count == 0
            || requiredPermissionKeys.Any(string.IsNullOrWhiteSpace))
        {
            throw new UnauthorizedAccessException(DeniedMessage);
        }

        bool isAuthorized = await _authorizationService.IsAuthorizedForAnyAsync(
            context,
            requiredPermissionKeys,
            cancellationToken);

        if (!isAuthorized)
        {
            throw new UnauthorizedAccessException(DeniedMessage);
        }
    }
}
