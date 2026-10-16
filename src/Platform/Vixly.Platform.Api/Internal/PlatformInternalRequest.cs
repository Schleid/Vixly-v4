using Microsoft.AspNetCore.Http;
using Vixly.Platform.Application.Security;

namespace Vixly.Platform.Api.Internal;

// =========================================================
// FILE: PlatformInternalRequest.cs
//
// PURPOSE:
// The checked caller of an internal request, as use-case contexts (Slice 9, D-K8).
//
// RESPONSIBILITIES:
// - Keep the token on the request
// - Build the tenant context from the token only
//
// IMPORTANT NOTES:
// - The tenant id never comes from the route or the body (S-2, S-13)
// =========================================================
public static class PlatformInternalRequest
{
    private const string ItemKey = "vixly.platform.internal.token";

    public static void Set(HttpContext http, PlatformServiceToken token)
    {
        ArgumentNullException.ThrowIfNull(http);
        http.Items[ItemKey] = token;
    }

    public static PlatformServiceToken? Find(HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(http);
        return http.Items.TryGetValue(ItemKey, out object? value) ? value as PlatformServiceToken : null;
    }

    public static PlatformServiceToken Token(HttpContext http)
    {
        return Find(http) ?? throw new InvalidOperationException("Internal route reached without a checked token.");
    }

    public static PlatformTenantAuthorizationContext Tenant(HttpContext http)
    {
        PlatformServiceToken token = Token(http);

        return new PlatformTenantAuthorizationContext(
            token.TenantId ?? throw new InvalidOperationException("Tenant route reached without a tenant."),
            token.Subject,
            token.Name,
            PlatformTenantAccess.ParseRole(token.Role),
            token.StepUp,
            token.Caller,
            token.ClientIp,
            token.UserAgent);
    }

    public static string PartitionKey(HttpContext http, string suffix = "")
    {
        PlatformServiceToken? token = Find(http);
        return token is null ? "unauthenticated" : token.Caller + ":" + (token.TenantId?.ToString("N") ?? "-") + suffix;
    }
}
