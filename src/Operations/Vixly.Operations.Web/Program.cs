using Vixly.Operations.Web.Hosting;

namespace Vixly.Operations.Web;

// =========================================================
// FILE: Program.cs
//
// PURPOSE:
// Entry point of Vixly.Operations.Web.
//
// RESPONSIBILITIES:
// - Build and run the staff host
//
// IMPORTANT NOTES:
// - Composition lives in OperationsWebHost so tests build the same pipeline
// =========================================================
public static class Program
{
    public static async Task Main(string[] args)
    {
        WebApplication app = OperationsWebHost.Build(args);

        await app.RunAsync();
    }
}
