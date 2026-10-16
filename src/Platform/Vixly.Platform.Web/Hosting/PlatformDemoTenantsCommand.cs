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
// - Run PlatformDemoTenants, then PlatformDemoSubscriptions,
//   then PlatformDemoSales, then PlatformDemoBilling, then
//   PlatformDemoLicensing (Slice 7), then PlatformDemoLegal
//   (Slice 8), and print
//   what was added
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

        PlatformDemoSubscriptions subscriptions = ActivatorUtilities.CreateInstance<PlatformDemoSubscriptions>(scope.ServiceProvider);
        Console.WriteLine(await subscriptions.SeedAsync(CancellationToken.None));

        PlatformDemoSales sales = ActivatorUtilities.CreateInstance<PlatformDemoSales>(scope.ServiceProvider);
        Console.WriteLine(await sales.SeedAsync(CancellationToken.None));

        PlatformDemoBilling billing = ActivatorUtilities.CreateInstance<PlatformDemoBilling>(scope.ServiceProvider);
        Console.WriteLine(await billing.SeedAsync(CancellationToken.None));

        PlatformDemoLicensing licensing = ActivatorUtilities.CreateInstance<PlatformDemoLicensing>(scope.ServiceProvider);
        Console.WriteLine(await licensing.SeedAsync(CancellationToken.None));

        PlatformDemoLegal legal = ActivatorUtilities.CreateInstance<PlatformDemoLegal>(scope.ServiceProvider);
        Console.WriteLine(await legal.SeedAsync(CancellationToken.None));
        return 0;
    }
}
