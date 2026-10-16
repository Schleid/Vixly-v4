using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vixly.Platform.Api.Endpoints;
using Vixly.Platform.Api.Hosting;
using Vixly.Platform.Application.Security;

namespace Vixly.Platform.Api.Internal;

// =========================================================
// FILE: PlatformInternalAuthentication.cs
//
// PURPOSE:
// Guards every /internal route: internal host, service token, scope and tenant (Slice 9, D-K1, D-K2).
//
// RESPONSIBILITIES:
// - Answer 404 off the internal hosts or for unknown routes
// - Answer 401 or 403 with an error body for a refused token
// - Never cache internal answers unless the route says so
//
// IMPORTANT NOTES:
// - Runs after routing and before rate limiting, so limits are per caller and tenant (D-K6)
// - Forwarded headers are not used to decide the host
// =========================================================
public static class PlatformInternalAuthentication
{
    public const string CorrelationHeader = "X-Correlation-Id";

    public static IApplicationBuilder UsePlatformInternalAuthentication(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (http, next) =>
        {
            if (!http.Request.Path.StartsWithSegments("/internal"))
            {
                await next(http);
                return;
            }

            PlatformInternalOptions options = http.RequestServices.GetRequiredService<IOptions<PlatformInternalOptions>>().Value;
            PlatformInternalEndpointMetadata? rule = http.GetEndpoint()?.Metadata.GetMetadata<PlatformInternalEndpointMetadata>();

            if (!options.Enabled || rule is null || !options.Hosts.Contains(http.Request.Host.Host, StringComparer.OrdinalIgnoreCase))
            {
                http.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            http.Response.Headers.CacheControl = "no-store";
            http.Response.Headers[CorrelationHeader] = PlatformApiErrors.CorrelationId(http);
            PlatformApiErrors errors = http.RequestServices.GetRequiredService<PlatformApiErrors>();
            string authorization = http.Request.Headers.Authorization.ToString();
            PlatformServiceToken? token = authorization.StartsWith("Bearer ", StringComparison.Ordinal)
                ? http.RequestServices.GetRequiredService<PlatformServiceTokenValidator>().Validate(authorization["Bearer ".Length..].Trim())
                : null;

            if (token is null)
            {
                http.Response.Headers.WWWAuthenticate = "Bearer";
                await errors.InternalProblem(PlatformInternalErrors.TokenInvalid).ExecuteAsync(http);
                return;
            }

            if (!token.Scopes.Contains(rule.Scope, StringComparer.Ordinal))
            {
                await errors.InternalProblem(PlatformInternalErrors.ScopeNotAllowed).ExecuteAsync(http);
                return;
            }

            if (rule.TenantRequired && token.TenantId is null)
            {
                await errors.InternalProblem(PlatformInternalErrors.TenantRequired).ExecuteAsync(http);
                return;
            }

            PlatformInternalRequest.Set(http, token);
            await next(http);
        });
    }
}
