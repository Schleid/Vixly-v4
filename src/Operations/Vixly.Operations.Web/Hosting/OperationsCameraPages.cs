using System.Text.RegularExpressions;

namespace Vixly.Operations.Web.Hosting;

// =========================================================
// FILE: OperationsCameraPages.cs
//
// PURPOSE:
// The station pages a handheld or phone may scan with its camera (Slice 12D, owner H-7).
//
// RESPONSIBILITIES:
// - Allow the camera (Permissions-Policy) on those pages only; every other page keeps it off
//
// IMPORTANT NOTES:
// - The same list as ops-camera-field.js, which adds the camera button to their scan fields
// - The dock scan page sets its own policy (Slice 3B)
// =========================================================
public static partial class OperationsCameraPages
{
    public const string Policy = "camera=(self), microphone=(), geolocation=(), payment=()";

    [GeneratedRegex("^/staff/(inspection|parcels/(move|shelf-counts|counts|inspect)|bundles|arrivals|pickup)(/|$)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex Stations();

    public static IApplicationBuilder UseOperationsCameraPages(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (context, next) =>
        {
            if (HttpMethods.IsGet(context.Request.Method) && Stations().IsMatch(context.Request.Path.Value ?? string.Empty))
            {
                context.Items[Vixly.Shared.Hosting.VixlySecurityHeaders.PermissionsPolicyItem] = Policy;
            }

            await next();
        });
    }
}
