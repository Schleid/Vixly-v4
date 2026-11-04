using Microsoft.AspNetCore.Antiforgery;

namespace Vixly.Operations.Web.Security;

// =========================================================
// FILE: OperationsFormGuard.cs
//
// PURPOSE:
// Refuses a staff POST whose antiforgery token is missing or wrong, even when the form has no other field (ADR-0019).
//
// RESPONSIBILITIES:
// - Ask the antiforgery middleware to check every change under /staff
// - Answer 400 when that check failed
//
// IMPORTANT NOTES:
// - ASP.NET Core rejects a bad token only when an endpoint reads the form; an endpoint with no form field would run anyway
// =========================================================
public static class OperationsFormGuard
{
    public static RouteGroupBuilder RequireFormToken(this RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.WithMetadata(new RequireAntiforgeryTokenAttribute());
        group.AddEndpointFilter(async (context, next) =>
            context.HttpContext.Features.Get<IAntiforgeryValidationFeature>() is { IsValid: false }
                ? Results.BadRequest()
                : await next(context));
        return group;
    }
}
