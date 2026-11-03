namespace Vixly.Public.Web.Hosting;

// =========================================================
// FILE: PublicHelpRouting.cs
//
// PURPOSE:
// Renders help's missing articles through the normal 404 page.
//
// RESPONSIBILITIES:
// - Give a second Razor render its own request service scope.
//
// IMPORTANT NOTES:
// - Applies only to help; existing application error routes are unchanged.
// - Install inside the global status-code middleware, before endpoints.
// =========================================================
public static class PublicHelpRouting
{
    public static IApplicationBuilder UsePublicHelpNotFound(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseWhen(http => PublicLanguage.FromPath(http.Request.Path) is { } language
            && http.Request.Path.StartsWithSegments($"/{language}/help"), branch =>
                branch.UseStatusCodePagesWithReExecute("/not-found", queryFormat: null, createScopeForStatusCodePages: true));
    }
}
