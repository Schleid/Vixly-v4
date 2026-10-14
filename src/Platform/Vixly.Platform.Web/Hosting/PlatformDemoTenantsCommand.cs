namespace Vixly.Platform.Web.Hosting;

// =========================================================
// FILE: PlatformDemoTenantsCommand.cs
//
// PURPOSE:
// The development command that fills the database with demo
// tenants.
//
// RESPONSIBILITIES:
// - Refuse outside the Development environment
// - Run PlatformDemoTenants and print what was added
//
// IMPORTANT NOTES:
// - Usage: dotnet run -- --seed-demo-tenants
// - Run after the first SuperUser has accepted the invitation
// - Safe to run twice: the second run adds nothing
// =========================================================
public static class PlatformDemoTenantsCommand
{
    public const string Switch = "--seed-demo-tenants";

    public static async Task<int> RunAsync(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsDevelopment())
        {
            Console.Error.WriteLine("Demo tenants are for the Development environment only.");
            return 2;
        }

        using IServiceScope scope = app.Services.CreateScope();
        PlatformDemoTenants demo = ActivatorUtilities.CreateInstance<PlatformDemoTenants>(scope.ServiceProvider);

        Console.WriteLine(await demo.SeedAsync(CancellationToken.None));
        return 0;
    }
}
