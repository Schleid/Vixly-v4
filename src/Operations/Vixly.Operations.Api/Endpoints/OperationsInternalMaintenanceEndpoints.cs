using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vixly.Contracts.Operations.Internal;
using Vixly.Contracts.Platform.Maintenance;
using Vixly.Operations.Api.Internal;
using Vixly.Operations.Infrastructure.Platform;

namespace Vixly.Operations.Api.Endpoints;

// =========================================================
// FILE: OperationsInternalMaintenanceEndpoints.cs
//
// PURPOSE:
// The maintenance windows of the customer portals, for Customer.Web (Control backlog 2026-10-22, section 1).
//
// RESPONSIBILITIES:
// - Map GET /internal/v1/maintenance and POST /internal/v1/maintenance/pass
//
// IMPORTANT NOTES:
// - Customer scope, no tenant: the window closes every portal
// - Platform unreachable: an empty list, so the portals stay open
// =========================================================
public static class OperationsInternalMaintenanceEndpoints
{
    private const string Site = "customers";

    public static IEndpointRouteBuilder MapOperationsInternalMaintenanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder maintenance = endpoints.MapGroup("/internal/v1/maintenance")
            .WithMetadata(new OperationsInternalEndpointMetadata(OperationsCallerClaims.CustomerScope, false));

        maintenance.MapGet(string.Empty, async (OperationsMaintenanceReader reader, CancellationToken cancellationToken) =>
        {
            IReadOnlyList<PlatformMaintenanceWindowInfo> windows = await reader.ReadAsync(Site, cancellationToken) ?? Array.Empty<PlatformMaintenanceWindowInfo>();
            return Results.Ok(windows.Select(window => new OperationsMaintenanceWindowDto(window.Id, window.StartsUtc, window.EndsUtc, window.MessageEn, window.MessageFr, window.MessageEs)).ToArray());
        });

        maintenance.MapPost("/pass", async (OperationsMaintenancePassDto body, OperationsMaintenanceReader reader, CancellationToken cancellationToken) =>
            string.IsNullOrWhiteSpace(body.Token) || await reader.CheckPassAsync(Site, body.Token, cancellationToken) is not { } endsUtc
                ? Results.NotFound()
                : Results.Ok(new OperationsMaintenancePassDto(null, endsUtc)));

        return endpoints;
    }
}
