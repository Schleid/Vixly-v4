using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vixly.Operations.Infrastructure.DependencyInjection;
using Vixly.Tools.V3Migration;

// =========================================================
// FILE: Program.cs
//
// PURPOSE:
// Copies one company's people and open work from V3 into V4 (owner, 2026-10-23).
//
// HOW TO RUN (from the V4 folder, on the owner's PC):
//   $env:V3_DB="<V3 connection string>"        (a restored copy, or production read-only)
//   $env:V4_DB="<V4 Operations connection string>"
//   dotnet run --project build/tools/V3Migration -- --tenant <V4 company id> [--apply] [--v3-tenant <id>] [--units kg]
//   Add --addresses-only to copy only the customers' home addresses, for a company already migrated (owner, 2026-10-26)
//
// IMPORTANT NOTES:
// - Without --apply it is a dry run: everything is checked inside a transaction that is rolled back
// - Connection strings come from environment variables only, never arguments or files
// - Writes the report next to where it runs: v3-migration-<time>.txt
// =========================================================
string? Value(string name) => args.SkipWhile(arg => !string.Equals(arg, name, StringComparison.OrdinalIgnoreCase)).Skip(1).FirstOrDefault();

string v3 = Environment.GetEnvironmentVariable("V3_DB") ?? throw new InvalidOperationException("Set V3_DB to the V3 connection string.");
string v4 = Environment.GetEnvironmentVariable("V4_DB") ?? throw new InvalidOperationException("Set V4_DB to the V4 Operations connection string.");
Guid tenant = Guid.TryParse(Value("--tenant"), out Guid parsed) ? parsed : throw new InvalidOperationException("Give --tenant, the V4 company id (Control, the company's page).");
Guid? v3Tenant = Guid.TryParse(Value("--v3-tenant"), out Guid chosen) ? chosen : null;
bool apply = args.Contains("--apply", StringComparer.OrdinalIgnoreCase);
bool addressesOnly = args.Contains("--addresses-only", StringComparer.OrdinalIgnoreCase);
bool pounds = !string.Equals(Value("--units"), "kg", StringComparison.OrdinalIgnoreCase);

HostApplicationBuilder builder = Host.CreateApplicationBuilder(Array.Empty<string>());
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ConnectionStrings:Operations"] = v4,
    // The tool writes no files; the setting only satisfies the file store's registration.
    ["OperationsFiles:Root"] = Path.GetTempPath()
});
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Critical);
builder.Services.AddOperationsInfrastructure(builder.Configuration, "Vixly.V4.Operations.Web");
using IHost host = builder.Build();

V3Snapshot snapshot = await V3Snapshot.ReadAsync(v3, v3Tenant, CancellationToken.None);
Report report = new();
report.Line("V3: " + snapshot.CompanyName + ": " + snapshot.Staff.Count + " staff, " + snapshot.Customers.Count + " customers, " + snapshot.Warehouses.Count + " warehouses, "
    + snapshot.Shipments.Count + " open shipments, " + snapshot.Parcels.Count + " parcels, " + snapshot.Invoices.Count + " invoices.");
report.Line((apply ? "Mode: APPLY" : "Mode: dry run") + (addressesOnly ? ", addresses only" : string.Empty));
report.Line("Units in V3: " + (pounds ? "lb and in" : "kg and cm"));

int exit = 0;

try
{
    await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
    await new Migrator(scope.ServiceProvider, report, pounds).RunAsync(snapshot, tenant, apply, CancellationToken.None, addressesOnly);
}
catch (Exception exception)
{
    report.Line("STOPPED, nothing saved: " + exception.Message);
    exit = 1;
}

string text = report.Render();
Console.WriteLine(text);
string file = "v3-migration-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture) + ".txt";
await File.WriteAllTextAsync(file, text);
Console.WriteLine("Report saved: " + file);
return exit != 0 ? exit : report.Problems > 0 ? 2 : 0;
