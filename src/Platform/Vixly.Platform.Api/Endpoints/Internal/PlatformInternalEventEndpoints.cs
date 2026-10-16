using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vixly.Contracts.Platform.Events;
using Vixly.Platform.Api.Internal;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Events.ReadPlatformEvents;

namespace Vixly.Platform.Api.Endpoints.Internal;

// =========================================================
// FILE: PlatformInternalEventEndpoints.cs
//
// PURPOSE:
// The public event feed with a cursor (Slice 9, D-K3).
//
// RESPONSIBILITIES:
// - Return events after the cursor in order
// - Show Edge only the events it needs
//
// IMPORTANT NOTES:
// - A caller without the tenant scope (Edge) reads the tenant, slug and subscription status events only
// =========================================================
public static class PlatformInternalEventEndpoints
{
    public static void Map(RouteGroupBuilder events)
    {
        ArgumentNullException.ThrowIfNull(events);

        events.MapGet(string.Empty, async (long? after, int? limit, ReadPlatformEventsUseCase useCase, HttpContext http, PlatformApiErrors errors, CancellationToken cancellationToken) =>
        {
            PlatformServiceToken token = PlatformInternalRequest.Token(http);
            IReadOnlyCollection<string> types = token.Scopes.Contains(PlatformInternalScopes.Tenant, StringComparer.Ordinal) ? PlatformEventTypes.All.ToArray() : PlatformEventTypes.ForEdge.ToArray();
            ReadPlatformEventsResponse response = await useCase.ExecuteAsync(PlatformInternalAuthorizationContext.Instance,
                new ReadPlatformEventsRequest { After = after ?? 0, Limit = limit ?? 200, Types = types }, cancellationToken);
            return response.Succeeded
                ? Results.Ok(new PlatformEventPageInfo(response.Items.Select(PlatformInternalMapping.Event).ToArray(), response.Next, response.More))
                : errors.InternalProblem(response.Error!);
        });
    }
}
