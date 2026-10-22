using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vixly.Contracts.Operations.Internal;
using Vixly.Operations.Api.Internal;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tenancy;
using Vixly.Operations.Application.UseCases.Tenancy.ResolveOperationsTenant;

namespace Vixly.Operations.Api.Endpoints;

// =========================================================
// FILE: OperationsInternalTenantEndpoints.cs
//
// PURPOSE:
// Internal endpoints about tenants for Customer.Web (Stage 3, Slice 0).
//
// RESPONSIBILITIES:
// - GET /internal/v1/tenants/by-host/{slug}: which tenant a host name belongs to
//
// IMPORTANT NOTES:
// - Scope customer; no tenant claim needed (the tenant is what is asked)
// - Customers never see a tenant still being set up: it reads as not found to them
// =========================================================
public static class OperationsInternalTenantEndpoints
{
    public static IEndpointRouteBuilder MapOperationsInternalTenantEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder tenants = endpoints.MapGroup("/internal/v1/tenants")
            .WithMetadata(new OperationsInternalEndpointMetadata(OperationsCallerClaims.CustomerScope, false));

        tenants.MapGet("/by-host/{slug}", async (string slug, ResolveOperationsTenantUseCase useCase, HttpContext http, CancellationToken cancellationToken) =>
        {
            ResolveOperationsTenantResponse response = await useCase.ExecuteAsync(OperationsAuthorizationContext.Host, new ResolveOperationsTenantRequest { Slug = slug }, cancellationToken);

            if (!response.Succeeded)
            {
                return OperationsApiErrors.Problem(http, response.Error!);
            }

            OperationsTenantHost host = response.Host!.State == OperationsTenantState.SettingUp ? OperationsTenantHost.NotFound : response.Host;
            return Results.Ok(new OperationsTenantHostDto(
                host.State != OperationsTenantState.NotFound,
                host.TenantId,
                Code(host.State),
                Code(host.Access),
                host.CanonicalSlug,
                host.DefaultLanguage));
        });

        return endpoints;
    }

    private static string Code(OperationsTenantState state) => state switch
    {
        OperationsTenantState.Live => "live",
        OperationsTenantState.SettingUp => "setting_up",
        OperationsTenantState.Suspended => "suspended",
        _ => "not_found"
    };

    private static string Code(OperationsTenantAccess access) => access switch
    {
        OperationsTenantAccess.Full => "full",
        OperationsTenantAccess.ReadOnly => "read_only",
        OperationsTenantAccess.PaymentRequired => "payment_required",
        _ => "none"
    };
}
