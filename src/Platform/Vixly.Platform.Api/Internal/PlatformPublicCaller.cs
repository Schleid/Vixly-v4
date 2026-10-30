using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Vixly.Platform.Api.Internal;

// =========================================================
// FILE: PlatformPublicCaller.cs
//
// PURPOSE:
// Reads the visitor IP that Public.Web signs on its server-to-server calls (Stage 2).
//
// RESPONSIBILITIES:
// - Check the X-Vixly-Caller token on public routes
// - Use its ip claim as the client IP when the caller has the public scope
//
// IMPORTANT NOTES:
// - Authorization stays free for the registration access token
// - An invalid token is ignored (the connection IP is used) and logged once per request
// =========================================================
public static class PlatformPublicCaller
{
    public const string Header = "X-Vixly-Caller";

    public static IApplicationBuilder UsePlatformPublicCaller(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (http, next) =>
        {
            if (!http.Request.Path.StartsWithSegments("/internal") && http.Request.Headers.TryGetValue(Header, out var values) && values.Count == 1)
            {
                PlatformServiceToken? token = http.RequestServices.GetRequiredService<PlatformServiceTokenValidator>().Validate(values[0]);

                if (token is not null && token.Scopes.Contains(PlatformInternalScopes.Public, StringComparer.Ordinal) && IPAddress.TryParse(token.ClientIp, out IPAddress? visitor))
                {
                    PlatformClientAddress.SetVisitor(http, visitor.ToString());
                }
                else
                {
                    http.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(PlatformPublicCaller).FullName!)
                        .LogWarning("A public caller token was refused; the connection address is used for limits.");
                }
            }

            await next(http);
        });
    }
}
