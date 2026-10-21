using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Vixly.Shared.Hosting;

// =========================================================
// FILE: VixlySecurityHeaders.cs
//
// PURPOSE:
// The one implementation of the security headers every V4
// host sends (Global Standard S-10, ADR-0011).
//
// RESPONSIBILITIES:
// - Content-Security-Policy, X-Content-Type-Options,
//   X-Frame-Options, Referrer-Policy, Permissions-Policy,
//   Cross-Origin-Opener-Policy
// - HSTS outside Development
//
// IMPORTANT NOTES:
// - Tracked CSP exception: style-src 'unsafe-inline', needed
//   by Blazor and FluentUI inline styles. Scripts stay 'self'
//   only: no inline script, no eval
// - img-src allows data: for generated QR codes
// - frame-ancestors 'none': no V4 page may be framed
// - Permissions-Policy denies the camera everywhere except a page that sets its own (the Operations scan station)
// =========================================================
public static class VixlySecurityHeaders
{
    public const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "object-src 'none'";

    public const string PermissionsPolicy = "camera=(), microphone=(), geolocation=(), payment=()";

    // A page that needs the camera (a barcode scan page) sets its own policy in HttpContext.Items under this key.
    public const string PermissionsPolicyItem = "vx.permissions-policy";

    public static WebApplication UseVixlySecurityHeaders(this WebApplication app)
    {
        return UseVixlySecurityHeaders(app, ContentSecurityPolicy);
    }

    // A host that needs a wider policy (the public site: bot check and card checkout) passes its own.
    public static WebApplication UseVixlySecurityHeaders(this WebApplication app, string contentSecurityPolicy)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentSecurityPolicy);

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                IHeaderDictionary headers = context.Response.Headers;
                // A response that set its own policy keeps it (a file download in a sandbox); every other gets the site's.
                if (string.IsNullOrEmpty(headers.ContentSecurityPolicy))
                {
                    headers["Content-Security-Policy"] = contentSecurityPolicy;
                }

                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                headers["Permissions-Policy"] = context.Items.TryGetValue(PermissionsPolicyItem, out object? policy) && policy is string own ? own : PermissionsPolicy;
                headers["Cross-Origin-Opener-Policy"] = "same-origin";
                return Task.CompletedTask;
            });

            await next(context);
        });

        return app;
    }
}
