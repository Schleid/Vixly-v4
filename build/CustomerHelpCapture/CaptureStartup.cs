namespace Vixly.CustomerHelpCapture;

// =========================================================
// FILE: CaptureStartup.cs
// PURPOSE:
// Resolves a fictional company behind the localhost certificate.
// RESPONSIBILITIES:
// - Keep capture-only host rewriting out of the deployed app.
// IMPORTANT NOTES:
// - This tool listens on loopback only and accepts no remote data.
// =========================================================
public sealed class CaptureStartup : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (http, continuation) =>
        {
            http.Request.Host = new HostString("bluewater-shipping.localhost", 7444);
            await continuation();
        });
        next(app);
    };
}
