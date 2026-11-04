using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vixly.Contracts.Platform.Maintenance;
using Vixly.Platform.Api.Hosting;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Maintenance.CheckPlatformMaintenancePass;
using Vixly.Platform.Application.UseCases.Maintenance.GetPlatformMaintenanceForSite;
using Vixly.Platform.Domain.Maintenance;

namespace Vixly.Platform.Api.Endpoints;

// =========================================================
// FILE: PlatformMaintenanceEndpoints.cs
//
// PURPOSE:
// What the sites read about maintenance windows (Control backlog 2026-10-22, section 1).
//
// RESPONSIBILITIES:
// - Map GET /maintenance?site= and POST /maintenance/pass
//
// IMPORTANT NOTES:
// - Anonymous: what GET returns is shown to every visitor anyway; windows further ahead stay private
// - The pass check is rate-limited per caller; a wrong link reads as 404
// - Cached by nobody: the sites keep their own copy for a minute
// =========================================================
public static class PlatformMaintenanceEndpoints
{
    public static IEndpointRouteBuilder MapPlatformMaintenanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/maintenance", async (string? site, GetPlatformMaintenanceForSiteUseCase useCase, PlatformApiErrors errors, HttpContext http, CancellationToken cancellationToken) =>
        {
            GetPlatformMaintenanceForSiteResponse response = await useCase.ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance,
                new GetPlatformMaintenanceForSiteRequest { Site = site ?? string.Empty }, cancellationToken);

            if (!response.Succeeded)
            {
                return errors.Problem(response.Error!);
            }

            http.Response.Headers.CacheControl = "no-store";

            return Results.Ok(response.Windows.Select(window => new PlatformMaintenanceWindowInfo
            {
                Id = window.Id,
                StartsUtc = window.StartsUtc,
                EndsUtc = window.EndsUtc,
                Running = window.Status == PlatformMaintenanceStatus.Running,
                MessageEn = window.MessageEn,
                MessageFr = window.MessageFr,
                MessageEs = window.MessageEs
            }).ToArray());
        }).RequireRateLimiting(PlatformApiRateLimits.Maintenance);

        endpoints.MapPost("/maintenance/pass", async (PlatformMaintenancePassBody body, CheckPlatformMaintenancePassUseCase useCase, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            CheckPlatformMaintenancePassResponse response = await useCase.ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance,
                new CheckPlatformMaintenancePassRequest { Token = body.Token, Site = body.Site }, cancellationToken);

            return response.Succeeded
                ? Results.Ok(new PlatformMaintenancePassInfo { EndsUtc = response.EndsUtc })
                : errors.Problem(response.Error!);
        }).RequireRateLimiting(PlatformApiRateLimits.MaintenancePass);

        return endpoints;
    }
}
