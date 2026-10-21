using Vixly.Customer.Web.Hosting;

namespace Vixly.Customer.Web;

// =========================================================
// FILE: Program.cs
//
// PURPOSE:
// Entry point of Vixly.Customer.Web.
//
// RESPONSIBILITIES:
// - Build and run the customer host
//
// IMPORTANT NOTES:
// - Composition lives in CustomerWebHost so tests build the same pipeline
// =========================================================
public static class Program
{
    public static async Task Main(string[] args)
    {
        WebApplication app = CustomerWebHost.Build(args);

        await app.RunAsync();
    }
}
