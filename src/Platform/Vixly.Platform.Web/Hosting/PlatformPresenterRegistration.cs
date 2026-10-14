using Vixly.Shared.UI.Presenters;

namespace Vixly.Platform.Web.Hosting;

// =========================================================
// FILE: PlatformPresenterRegistration.cs
//
// PURPOSE:
// Registers every console presenter with DI.
//
// RESPONSIBILITIES:
// - Find each concrete presenter in this assembly and register
//   it scoped, so each component scope gets its own (UI-4)
//
// IMPORTANT NOTES:
// - A presenter is any non-abstract PresenterBase subclass
// =========================================================
public static class PlatformPresenterRegistration
{
    public static IServiceCollection AddPlatformPresenters(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        foreach (Type presenter in typeof(PlatformPresenterRegistration).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.IsSubclassOf(typeof(PresenterBase))))
        {
            services.AddScoped(presenter);
        }

        services.AddScoped<Features.PlatformUserWorkspace>();
        services.AddScoped<Features.PlatformUseCaseScope>();

        return services;
    }
}
