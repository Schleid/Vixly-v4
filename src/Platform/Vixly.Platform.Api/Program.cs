using Vixly.Platform.Api.Hosting;

namespace Vixly.Platform.Api;

// =========================================================
// FILE: Program.cs
//
// PURPOSE:
// Entry point of Vixly.Platform.Api.
//
// RESPONSIBILITIES:
// - Build and run the API host
//
// IMPORTANT NOTES:
// - Composition lives in PlatformApiHost so tests build the same
//   pipeline
// =========================================================
public static class Program
{
    public static async Task Main(string[] args)
    {
        WebApplication app = PlatformApiHost.Build(args);

        await app.RunAsync();
    }
}
