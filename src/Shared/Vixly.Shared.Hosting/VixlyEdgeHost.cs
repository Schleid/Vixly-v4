using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Vixly.Shared.Hosting;

// =========================================================
// FILE: VixlyEdgeHost.cs
//
// PURPOSE:
// Lets the company address forwarded by the Cloudflare Worker
// (X-Forwarded-Host) through only when the Worker proves itself
// (side-by-side rollout on vixly.ltd).
//
// RESPONSIBILITIES:
// - Compare X-Vixly-Edge with Hosting:EdgeSecret in constant
//   time
// - Without the secret, drop X-Forwarded-Host, so nobody can
//   pose as another company's address
// - Always drop X-Vixly-Edge, so the secret never reaches the
//   application or its logs
// - Then run UseForwardedHeaders
//
// IMPORTANT NOTES:
// - Replaces app.UseForwardedHeaders() in every host
// - The host names allowed come from Hosting:EdgeHosts
//   (ForwardedHeadersOptions.AllowedHosts)
// =========================================================
public static class VixlyEdgeHost
{
    public const string SecretHeader = "X-Vixly-Edge";

    private const string ForwardedHost = "X-Forwarded-Host";

    public static IApplicationBuilder UseVixlyForwardedHeaders(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        VixlyEdgeSettings edge = app.ApplicationServices.GetRequiredService<VixlyEdgeSettings>();
        VixlyIisHop? hop = app.ApplicationServices.GetService<VixlyIisHop>();

        app.Use((http, next) =>
        {
            // Out of process, the peer is IIS until its hop is undone (once per request).
            hop?.Apply(http);

            string proof = http.Request.Headers[SecretHeader].ToString();
            http.Request.Headers.Remove(SecretHeader);

            if (!edge.Enabled || !Matches(proof, edge.Secret))
            {
                http.Request.Headers.Remove(ForwardedHost);
            }

            return next(http);
        });

        return app.UseForwardedHeaders();
    }

    private static bool Matches(string proof, string secret) =>
        proof.Length > 0 && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(proof), Encoding.UTF8.GetBytes(secret));
}
