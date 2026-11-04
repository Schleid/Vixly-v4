using Vixly.Operations.Api.Hosting;

namespace Vixly.Operations.Api;

// =========================================================
// FILE: Program.cs
//
// PURPOSE:
// Entry point of Vixly.Operations.Api.
//
// RESPONSIBILITIES:
// - Build and run the API host
//
// IMPORTANT NOTES:
// - Composition lives in OperationsApiHost so tests build the same pipeline
// =========================================================
public static class Program
{
    public static async Task Main(string[] args)
    {
        WebApplication app = OperationsApiHost.Build(args);

        await app.RunAsync();
    }
}
