using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vixly.Operations.Application.Security;

namespace Vixly.Operations.Api.Internal;

// =========================================================
// FILE: OperationsInternalAuthentication.cs
//
// PURPOSE:
// Lets only signed callers reach /internal on the internal host names (Stage 3, Slice 0, D-F4).
//
// RESPONSIBILITIES:
// - 404 for unknown host names or unmarked endpoints
// - 401 for a missing or broken token, 403 for a wrong scope or a missing tenant
// - Keep the checked token for the endpoint
//
// IMPORTANT NOTES:
// - Answers are never cached
// - Runs before any endpoint, so no handler sees an unchecked call
// =========================================================
public static class OperationsInternalAuthentication
{
    private const string TokenItem = "vx.ops.caller";

    public static IApplicationBuilder UseOperationsInternalAuthentication(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (http, next) =>
        {
            if (!http.Request.Path.StartsWithSegments("/internal"))
            {
                await next(http);
                return;
            }

            OperationsInternalOptions options = http.RequestServices.GetRequiredService<IOptions<OperationsInternalOptions>>().Value;
            OperationsInternalEndpointMetadata? rule = http.GetEndpoint()?.Metadata.GetMetadata<OperationsInternalEndpointMetadata>();

            if (rule is null || !options.Hosts.Contains(http.Request.Host.Host, StringComparer.OrdinalIgnoreCase))
            {
                http.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            http.Response.Headers.CacheControl = "no-store";
            http.Response.Headers[OperationsApiErrors.CorrelationHeader] = http.TraceIdentifier;
            string authorization = http.Request.Headers.Authorization.ToString();
            OperationsCallerToken? token = authorization.StartsWith("Bearer ", StringComparison.Ordinal)
                ? http.RequestServices.GetRequiredService<OperationsCallerTokenValidator>().Validate(authorization["Bearer ".Length..].Trim())
                : null;

            if (token is null)
            {
                http.Response.Headers.WWWAuthenticate = "Bearer";
                await OperationsApiErrors.Problem(http, OperationsInternalErrors.TokenInvalid).ExecuteAsync(http);
                return;
            }

            if (!token.Scopes.Contains(rule.Scope, StringComparer.Ordinal))
            {
                await OperationsApiErrors.Problem(http, OperationsInternalErrors.ScopeNotAllowed).ExecuteAsync(http);
                return;
            }

            if (rule.TenantRequired && token.TenantId is null)
            {
                await OperationsApiErrors.Problem(http, OperationsInternalErrors.TenantRequired).ExecuteAsync(http);
                return;
            }

            // Every query of the request runs inside the caller's tenant (D-F1).
            if (token.TenantId is { } tenantId)
            {
                http.RequestServices.GetRequiredService<Vixly.Operations.Application.Ports.IOperationsTenantScope>().Enter(tenantId);
            }

            http.Items[TokenItem] = token;
            await next(http);
        });
    }

    public static OperationsCallerToken Token(HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(http);
        return http.Items[TokenItem] as OperationsCallerToken ?? throw new InvalidOperationException("No checked caller token.");
    }
}
