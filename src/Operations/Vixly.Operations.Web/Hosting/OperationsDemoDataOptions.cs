namespace Vixly.Operations.Web.Hosting;

// =========================================================
// FILE: OperationsDemoDataOptions.cs
//
// PURPOSE:
// Settings of the Development demo data for Operations.
//
// RESPONSIBILITIES:
// - Switch the start-up seed on or off
// - List the Platform demo tenants to set up
//
// IMPORTANT NOTES:
// - Development only (OperationsDemoDataWorker)
// - The slugs match Platform's demo tenants (PlatformDemoTenants)
// =========================================================
public sealed class OperationsDemoDataOptions
{
    public const string SectionName = "OperationsDemoData";

    public bool SeedOnStartup { get; set; } = true;

    public IReadOnlyList<string> Slugs { get; set; } = Array.Empty<string>();

    // Demo staff: an Administrator and a front-desk clerk per tenant, and the Owner while still invited (Development only).
    public bool SeedStaff { get; set; } = true;

    public string Password { get; set; } = "Harbour demo 2026!";

    // Base32 key added once to an authenticator app; the Owner and Administrators of every demo tenant use it.
    public string AuthenticatorKey { get; set; } = "JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP";

    // A sample working day (OperationsDemoScenario): the first company gets the full story, the others a little data to check isolation.
    public IReadOnlyList<string> ScenarioSlugs { get; set; } = new[] { "bluewater-shipping", "andes-courier" };

    // How long a demo company Platform does not have yet is asked for again (a new database: Platform adds its demo tenants after the first SuperUser); 0 asks once.
    public int WaitForTenantsMinutes { get; set; } = 60;

    // Development has no licensed computers for the demo staff: the device gate lets them through.
    public bool SkipDeviceGate { get; set; }
}
