namespace Vixly.Platform.Application.Security;

// =========================================================
// FILE: IPlatformAuthorizationGate.cs
//
// PURPOSE:
// The Application-layer boundary that every protected Platform
// workflow passes before its use case runs.
//
// RESPONSIBILITIES:
// - Require an explicit trusted authorization context
// - Authorize against one required permission
// - Authorize against any permission in a set
//
// IMPORTANT NOTES:
// - Independent of HTTP, Blazor and any ambient user state
// - Throws UnauthorizedAccessException when access is denied;
//   hosts translate that to 403 without echoing the message
//   (Global Standard S-1, S-9)
// - Menu or navigation visibility is not authorization
// =========================================================
public interface IPlatformAuthorizationGate
{
    Task AuthorizeAsync(
        PlatformAuthorizationContext context,
        string requiredPermissionKey,
        CancellationToken cancellationToken = default);

    Task AuthorizeAnyAsync(
        PlatformAuthorizationContext context,
        IReadOnlyCollection<string> requiredPermissionKeys,
        CancellationToken cancellationToken = default);
}
