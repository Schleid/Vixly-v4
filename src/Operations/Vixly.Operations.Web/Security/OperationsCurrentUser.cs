using System.Security.Claims;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tenancy;
using Vixly.Operations.Web.Tenancy;

namespace Vixly.Operations.Web.Security;

// =========================================================
// FILE: OperationsCurrentUser.cs
//
// PURPOSE:
// Builds the authorization context of a request (Slice 1).
//
// RESPONSIBILITIES:
// - Tenant from the host, user and session from the checked cookie
//
// IMPORTANT NOTES:
// - Never from a form or a query (S-2)
// =========================================================
public static class OperationsCurrentUser
{
    public static OperationsAuthorizationContext Context(HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(http);

        OperationsTenantHost host = OperationsTenantMiddleware.Host(http) ?? throw new InvalidOperationException("No tenant for this request.");
        OperationsAuthorizationContext tenant = OperationsAuthorizationContext.ForTenant(
            host.TenantId ?? throw new InvalidOperationException("No tenant for this request."),
            host.State == OperationsTenantState.SettingUp);

        return Guid.TryParse(http.User.FindFirstValue(OperationsClaimTypes.UserId), out Guid userId)
            && Guid.TryParse(http.User.FindFirstValue(OperationsClaimTypes.SessionId), out Guid sessionId)
                ? tenant.WithUser(userId, sessionId)
                : tenant;
    }

    public static bool IsSignedIn(HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(http);
        return http.User.Identity?.IsAuthenticated == true;
    }

    public static string DisplayName(HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(http);
        return http.User.FindFirstValue(OperationsClaimTypes.DisplayName) ?? string.Empty;
    }
}
