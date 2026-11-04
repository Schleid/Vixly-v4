using Vixly.Public.Web.Features.Registration;

namespace Vixly.HelpCapture;

// =========================================================
// FILE: CaptureScenario.cs
//
// PURPOSE:
// Selects fictional registration screens for repeatable captures.
//
// RESPONSIBILITIES:
// - Set dummy state and redirect to the unchanged Public page.
//
// IMPORTANT NOTES:
// - Only compiled into the capture tool, not the application.
// - Synthetic values cannot authorize a real Platform request.
// =========================================================
public sealed class CaptureScenario : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (http, following) =>
        {
            if (!http.Request.Path.StartsWithSegments("/__capture", out PathString rest))
            {
                await following(http);
                return;
            }
            string[] parts = rest.Value!.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || parts[0] is not ("en" or "fr" or "es")
                || parts[1] is not ("email" or "plan" or "pay" or "done" or "verify"))
            {
                http.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
            RegistrationCookies.Write(http, RegistrationCookies.Access, "help-" + parts[1], null);
            http.Response.Redirect(parts[1] == "verify"
                ? "/register/verify?token=help-demo&lang=" + parts[0]
                : "/" + parts[0] + "/register/" + parts[1]);
        });
        next(app);
    };
}
