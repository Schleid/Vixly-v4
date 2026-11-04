using Microsoft.AspNetCore.Http;

namespace Vixly.Platform.Api.Internal;

// =========================================================
// FILE: PlatformClientAddress.cs
//
// PURPOSE:
// The client IP Platform.Api uses for limits, the bot check and audit (Stage 2).
//
// RESPONSIBILITIES:
// - Use the visitor IP Public.Web signed for, else the connection IP
//
// IMPORTANT NOTES:
// - A visitor IP is taken only from a valid service token with the public scope
// =========================================================
public static class PlatformClientAddress
{
    private const string ItemKey = "vixly.platform.public.visitor_ip";

    public static void SetVisitor(HttpContext http, string ip)
    {
        ArgumentNullException.ThrowIfNull(http);
        http.Items[ItemKey] = ip;
    }

    public static string? Of(HttpContext? http)
    {
        if (http is null)
        {
            return null;
        }

        return http.Items.TryGetValue(ItemKey, out object? value) && value is string visitor
            ? visitor
            : http.Connection.RemoteIpAddress?.ToString();
    }
}
