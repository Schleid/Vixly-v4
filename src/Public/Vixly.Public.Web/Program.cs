using Vixly.Public.Web.Hosting;

namespace Vixly.Public.Web;

// =========================================================
// FILE: Program.cs
//
// PURPOSE:
// Entry point of Vixly.Public.Web.
//
// RESPONSIBILITIES:
// - Build and run the public site host
//
// IMPORTANT NOTES:
// - Composition lives in PublicSiteHost so tests build the same pipeline
// =========================================================
public static class Program
{
    public static async Task Main(string[] args)
    {
        WebApplication app = PublicSiteHost.Build(args);

        await app.RunAsync();
    }
}
