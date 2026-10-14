using Microsoft.Extensions.DependencyInjection;
using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Departments;
using Vixly.Platform.Application.UseCases.Roles;
using Vixly.Platform.Application.UseCases.Users;

namespace Vixly.Platform.Application.DependencyInjection;

// =========================================================
// FILE: PlatformApplicationServiceCollectionExtensions.cs
//
// PURPOSE:
// Registers the Platform Application layer for a host's
// composition root.
//
// RESPONSIBILITIES:
// - Register the authorization gate and service, step-up
//   guard, audit trail and shared identity services
// - Register every use case in this assembly (types ending
//   in UseCase), scoped
//
// IMPORTANT NOTES:
// - The host registers IPlatformRequestMetadata; this layer
//   does not know about HTTP
// =========================================================
public static class PlatformApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IPlatformAuthorizationService, PlatformAuthorizationService>();
        services.AddScoped<IPlatformAuthorizationGate, PlatformAuthorizationGate>();
        services.AddScoped<IPlatformStepUpGuard, PlatformStepUpGuard>();
        services.AddScoped<PlatformAuditTrail>();
        services.AddScoped<PlatformSignInSteps>();
        services.AddScoped<PlatformStepUpSteps>();
        services.AddScoped<PlatformPasskeyCeremonies>();
        services.AddScoped<PlatformSessionValidator>();
        services.AddScoped<PlatformFirstSuperUserSetup>();
        services.AddScoped<PlatformUserAdministration>();
        services.AddScoped<PlatformUserLifecycleWorkflow>();
        services.AddScoped<PlatformRoleAdministration>();
        services.AddScoped<PlatformSystemRoleSynchronizer>();
        services.AddScoped<PlatformDepartmentAdministration>();

        foreach (Type useCase in typeof(PlatformApplicationServiceCollectionExtensions).Assembly
            .GetExportedTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.Name.EndsWith("UseCase", StringComparison.Ordinal)))
        {
            services.AddScoped(useCase);
        }

        return services;
    }
}
