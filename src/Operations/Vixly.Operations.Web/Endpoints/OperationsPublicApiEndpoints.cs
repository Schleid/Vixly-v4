using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.HttpResults;
using Vixly.Operations.Application.Api;
using Vixly.Operations.Application.Tenancy;
using Vixly.Operations.Application.UseCases.Api.AuthenticateOperationsApiKey;
using Vixly.Operations.Application.UseCases.Api.QueryOperationsApi;
using Vixly.Operations.Domain.Api;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Security;
using Vixly.Operations.Web.Tenancy;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsPublicApiEndpoints.cs
//
// PURPOSE:
// The company API (Slice 10; owner, 2026-10-22: read and webhooks): the company’s own programs read its data with a key.
//
// RESPONSIBILITIES:
// - GET /api/v1/{customers|shipments|parcels|invoices|payments}[/{id}], /api/v1/tracking, /api/v1/{shipments|parcels}/{id}/tracking
// - Bearer key checked against the company of the address called; JSON answers, oldest first with a cursor
//
// IMPORTANT NOTES:
// - Read only; at most 100 a page; 600 calls a minute per address, then 120 a minute per key once the key is checked
// - Errors carry a code only (unauthorized, not_in_plan, access_limited, not_found, rate_limited); never cached
// =========================================================
public static class OperationsPublicApiEndpoints
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // Per checked key (review: a key id made up on each call never gets a bucket of its own).
    private static readonly PartitionedRateLimiter<string> PerKey = PartitionedRateLimiter.Create<string, string>(key => RateLimitPartition.GetFixedWindowLimiter(key,
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

    private static readonly Dictionary<string, OperationsApiResource> Resources = new(StringComparer.Ordinal)
    {
        ["customers"] = OperationsApiResource.Customers,
        ["shipments"] = OperationsApiResource.Shipments,
        ["parcels"] = OperationsApiResource.Parcels,
        ["tracking"] = OperationsApiResource.Tracking,
        ["invoices"] = OperationsApiResource.Invoices,
        ["payments"] = OperationsApiResource.Payments
    };

    public static string? Bearer(HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(http);
        string header = http.Request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header["Bearer ".Length..].Trim() : null;
    }

    public static IEndpointRouteBuilder MapOperationsPublicApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder api = endpoints.MapGroup("/api/v1").AllowAnonymous().DisableAntiforgery().RequireRateLimiting(OperationsRateLimits.CompanyApi);
        api.AddEndpointFilter(async (context, next) =>
        {
            HttpContext http = context.HttpContext;
            http.Response.Headers.CacheControl = "no-store";

            if (OperationsTenantMiddleware.Host(http) is not { State: OperationsTenantState.Live } host)
            {
                return Error(StatusCodes.Status404NotFound, "not_found");
            }

            if (host.Access == OperationsTenantAccess.PaymentRequired)
            {
                return Error(StatusCodes.Status403Forbidden, "access_limited");
            }

            AuthenticateOperationsApiKeyResponse key = await http.RequestServices.GetRequiredService<AuthenticateOperationsApiKeyUseCase>()
                .ExecuteAsync(OperationsCurrentUser.Context(http), new AuthenticateOperationsApiKeyRequest { Token = Bearer(http) }, http.RequestAborted);

            if (!key.Succeeded)
            {
                if (key.Error == OperationsApiErrors.FeatureOff)
                {
                    return Error(StatusCodes.Status403Forbidden, "not_in_plan");
                }

                http.Response.Headers.WWWAuthenticate = "Bearer";
                return Error(StatusCodes.Status401Unauthorized, "unauthorized");
            }

            using RateLimitLease lease = PerKey.AttemptAcquire(host.TenantId + ":" + key.KeyId);

            if (!lease.IsAcquired)
            {
                http.Response.Headers.RetryAfter = "60";
                return Error(StatusCodes.Status429TooManyRequests, "rate_limited");
            }

            return await next(context);
        });

        api.MapGet("/{resource}", (HttpContext http, string resource, string? cursor, int? limit, Guid? customer_id, Guid? shipment_id, Guid? subject_id,
            QueryOperationsApiUseCase query) => ListAsync(http, resource, cursor, limit, customer_id, shipment_id, subject_id, query));
        api.MapGet("/{resource}/{id:guid}", async (HttpContext http, string resource, Guid id, QueryOperationsApiUseCase query) =>
        {
            if (!Resources.TryGetValue(resource, out OperationsApiResource kind) || kind == OperationsApiResource.Tracking)
            {
                return Error(StatusCodes.Status404NotFound, "not_found");
            }

            QueryOperationsApiResponse response = await query.ExecuteAsync(OperationsCurrentUser.Context(http), new QueryOperationsApiRequest { Resource = kind, Id = id },
                http.RequestAborted);
            return response.Succeeded ? Results.Json(new { data = response.Item }, Json) : Error(StatusCodes.Status404NotFound, "not_found");
        });
        api.MapGet("/{resource}/{id:guid}/tracking", (HttpContext http, string resource, Guid id, string? cursor, int? limit, QueryOperationsApiUseCase query) =>
            resource is "shipments" or "parcels" ? ListAsync(http, "tracking", cursor, limit, null, null, id, query, Resources[resource])
                : Task.FromResult(Error(StatusCodes.Status404NotFound, "not_found")));

        return endpoints;
    }

    private static async Task<IResult> ListAsync(HttpContext http, string resource, string? cursor, int? limit, Guid? customerId, Guid? shipmentId, Guid? subjectId,
        QueryOperationsApiUseCase query, OperationsApiResource? subjectResource = null)
    {
        if (!Resources.TryGetValue(resource, out OperationsApiResource kind))
        {
            return Error(StatusCodes.Status404NotFound, "not_found");
        }

        QueryOperationsApiResponse response = await query.ExecuteAsync(OperationsCurrentUser.Context(http), new QueryOperationsApiRequest
        {
            Resource = kind,
            Cursor = cursor,
            Limit = limit ?? 50,
            CustomerId = customerId,
            ShipmentId = shipmentId,
            SubjectId = subjectId,
            SubjectResource = subjectResource
        }, http.RequestAborted);
        return response.Succeeded ? Results.Json(new { data = response.Items, nextCursor = response.NextCursor }, Json) : Error(StatusCodes.Status404NotFound, "not_found");
    }

    private static IResult Error(int status, string code) => Results.Json(new { error = new { code } }, Json, statusCode: status);
}
