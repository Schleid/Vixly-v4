using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Vixly.PrintAgent.Printing;

namespace Vixly.PrintAgent;

// =========================================================
// FILE: Program.cs
//
// PURPOSE:
// Entry point of the dock print program (Slice 3B).
//
// RESPONSIBILITIES:
// - Run as a console program or a Windows service
//
// IMPORTANT NOTES:
// - Settings: PrintAgent:OperationsUrl (the company host), PrintAgent:AgentKey (from Operations, Printers page); the key belongs in user secrets or the service environment, never in a shared file
// =========================================================
public static class Program
{
    public static async Task Main(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddWindowsService(options => options.ServiceName = "Vixly print agent");
        builder.Services.AddOptions<PrintAgentOptions>().Bind(builder.Configuration.GetSection(PrintAgentOptions.SectionName))
            .Validate(options => Uri.TryCreate(options.OperationsUrl, UriKind.Absolute, out Uri? url) && url!.Scheme == Uri.UriSchemeHttps, "PrintAgent:OperationsUrl must be the https address of the company.")
            .Validate(options => options.AgentKey.StartsWith("vxpa.", StringComparison.Ordinal), "PrintAgent:AgentKey is missing.")
            .ValidateOnStart();
        builder.Services.AddHttpClient(PrintAgentWorker.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(20));
        builder.Services.AddSingleton<ILabelPrinterPort, NetworkLabelPrinter>();
        builder.Services.AddSingleton<ILabelPrinterPort, InstalledLabelPrinter>();
        builder.Services.AddHostedService<PrintAgentWorker>();

        await builder.Build().RunAsync();
    }
}
