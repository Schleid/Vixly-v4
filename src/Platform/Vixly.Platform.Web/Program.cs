using Vixly.Platform.Web.Hosting;

namespace Vixly.Platform.Web;

// =========================================================
// FILE: Program.cs
//
// PURPOSE:
// Entry point of the Platform console (Vixly Control).
//
// RESPONSIBILITIES:
// - Run the one-time first-SuperUser setup when asked
//   (--setup-first-superuser), otherwise start the web host
//
// IMPORTANT NOTES:
// - Composition and pipeline live in PlatformWebHost, so
//   this file stays a few lines
// - The host never migrates the database; run
//   build/scripts/sql/vixly-v4-database.sql first (D-1)
// =========================================================
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        WebApplication app = PlatformWebHost.Build(args);

        if (args.Contains(PlatformFirstSuperUserCommand.Switch, StringComparer.OrdinalIgnoreCase))
        {
            return await PlatformFirstSuperUserCommand.RunAsync(app, args);
        }

        await app.RunAsync();

        return 0;
    }
}
