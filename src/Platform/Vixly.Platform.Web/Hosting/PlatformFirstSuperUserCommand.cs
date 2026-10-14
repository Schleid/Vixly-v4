using Vixly.Platform.Application.Identity;
using Vixly.Platform.Infrastructure.Hosting;
using Vixly.Platform.Web.Endpoints;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Microsoft.Extensions.Options;

namespace Vixly.Platform.Web.Hosting;

// =========================================================
// FILE: PlatformFirstSuperUserCommand.cs
//
// PURPOSE:
// The one-time command that invites the first SuperUser on
// an empty database.
//
// RESPONSIBILITIES:
// - Read --email, --first-name and --last-name
// - Run PlatformFirstSuperUserSetup and print the invitation
//   link to the console
//
// IMPORTANT NOTES:
// - Usage:
//   dotnet run -- --setup-first-superuser --email you@x.com
//     --first-name Jude --last-name Owner
// - Refuses when any staff user exists
// - The link is printed once, to the operator's console only,
//   and works for 72 hours
// =========================================================
public static class PlatformFirstSuperUserCommand
{
    public const string Switch = "--setup-first-superuser";

    public static async Task<int> RunAsync(WebApplication app, string[] args)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(args);

        string? email = Value(args, "--email");
        string? firstName = Value(args, "--first-name");
        string? lastName = Value(args, "--last-name");

        if (email is null || firstName is null || lastName is null)
        {
            Console.Error.WriteLine($"Usage: {Switch} --email <email> --first-name <name> --last-name <name>");
            return 2;
        }

        using IServiceScope scope = app.Services.CreateScope();
        PlatformFirstSuperUserSetup setup = scope.ServiceProvider.GetRequiredService<PlatformFirstSuperUserSetup>();
        string baseUrl = scope.ServiceProvider.GetRequiredService<IOptions<PlatformHostingOptions>>().Value.ConsoleBaseUrl;
        PlatformErrorText text = scope.ServiceProvider.GetRequiredService<PlatformErrorText>();

        try
        {
            (string? token, Error? error) = await setup.RunAsync(firstName, lastName, email, CancellationToken.None);

            if (error is not null)
            {
                Console.Error.WriteLine($"Setup refused: {text.Describe(error)} ({error.Code})");
                return 1;
            }

            Console.WriteLine("First SuperUser invited. Open this link within 72 hours:");
            Console.WriteLine($"{baseUrl.TrimEnd('/')}/invitation?token={Uri.EscapeDataString(token!)}");
            return 0;
        }
        catch (DomainException exception)
        {
            Console.Error.WriteLine($"Setup refused: {text.Describe(exception.Error)} ({exception.Error.Code})");
            return 1;
        }
    }

    private static string? Value(string[] args, string name)
    {
        int index = Array.FindIndex(args, arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));

        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
