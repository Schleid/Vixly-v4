using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vixly.Contracts.Platform.Internal.Entitlements;
using Vixly.Contracts.Platform.Internal.Portal;
using Vixly.Platform.Api.Internal;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing.GetPlatformPortalAccess;
using Vixly.Platform.Application.UseCases.TenantAccount;
using Vixly.Platform.Application.UseCases.TenantAccount.GetPlatformEntitlementsBatch;

namespace Vixly.Platform.Api.Endpoints.Internal;

// =========================================================
// FILE: PlatformInternalPortalEndpoints.cs
//
// PURPOSE:
// Portal access for Edge and batch reads for Operations jobs (calls 1 and 14, D-K6).
//
// RESPONSIBILITIES:
// - Answer portal access by address, one or up to 100
// - Answer entitlements for up to 100 tenants
//
// IMPORTANT NOTES:
// - Portal answers may be kept 60 seconds
// =========================================================
public static class PlatformInternalPortalEndpoints
{
    public const string OperationsCaller = "operations";

    private static readonly PlatformInternalAuthorizationContext Internal = PlatformInternalAuthorizationContext.Instance;

    public static void Map(RouteGroupBuilder portal)
    {
        ArgumentNullException.ThrowIfNull(portal);

        portal.MapGet("/{slug}", async (string slug, GetPlatformPortalAccessUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            GetPlatformPortalAccessResponse response = await useCase.ExecuteAsync(Internal, new GetPlatformPortalAccessRequest { Slug = slug, IncludeSettingUp = SettingUpAllowed(http) }, cancellationToken);
            return response.Succeeded
                ? PlatformInternalCache.Ok(http, PlatformInternalMapping.Portal(response.Access!), PlatformInternalCache.PortalSeconds)
                : errors.InternalProblem(response.Error!);
        });

        portal.MapPost("/batch", async (PlatformPortalAccessBatchBody body, GetPlatformPortalAccessUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            string[] slugs = (body.Slugs ?? Array.Empty<string>()).Where(slug => !string.IsNullOrWhiteSpace(slug)).Select(slug => slug.Trim().ToLowerInvariant()).Distinct(StringComparer.Ordinal).ToArray();

            if (slugs.Length > PlatformTenantAccountLimits.BatchMax)
            {
                return errors.InternalProblem(PlatformInternalErrors.TooManyItems);
            }

            List<PlatformPortalAccessBatchItem> items = new(slugs.Length);

            foreach (string slug in slugs)
            {
                GetPlatformPortalAccessResponse response = await useCase.ExecuteAsync(Internal, new GetPlatformPortalAccessRequest { Slug = slug, IncludeSettingUp = SettingUpAllowed(http) }, cancellationToken);

                if (!response.Succeeded)
                {
                    return errors.InternalProblem(response.Error!);
                }

                items.Add(new PlatformPortalAccessBatchItem(slug, PlatformInternalMapping.Portal(response.Access!)));
            }

            return PlatformInternalCache.Ok(http, new PlatformPortalAccessBatchInfo(items), PlatformInternalCache.PortalSeconds);
        });
    }

    public static void MapJobs(RouteGroupBuilder jobs)
    {
        ArgumentNullException.ThrowIfNull(jobs);

        jobs.MapPost("/entitlements", async (PlatformEntitlementsBatchBody body, GetPlatformEntitlementsBatchUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            GetPlatformEntitlementsBatchResponse response = await useCase.ExecuteAsync(Internal,
                new GetPlatformEntitlementsBatchRequest { PlatformTenantIds = body.TenantIds ?? Array.Empty<Guid>() }, cancellationToken);
            return response.Succeeded
                ? PlatformInternalCache.Ok(http, new PlatformEntitlementsBatchInfo(response.Items.Select(PlatformInternalMapping.Entitlements).ToArray()), PlatformInternalCache.TenantSeconds)
                : errors.InternalProblem(response.Error!);
        });
    }

    // Only the Operations caller may see a tenant that is still being set up (Stage 3, Slice 0).
    private static bool SettingUpAllowed(HttpContext http)
    {
        return PlatformInternalRequest.Find(http) is { Caller: OperationsCaller };
    }
}
