using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vixly.Operations.Api.Endpoints;
using Vixly.Operations.Api.Internal;
using Vixly.Operations.Infrastructure.DependencyInjection;
using Vixly.Shared.Hosting;

namespace Vixly.Operations.Api.Hosting;

// =========================================================
// FILE: OperationsApiHost.cs
//
// PURPOSE:
// Builds the Operations.Api pipeline (Stage 3, Slice 0).
//
// RESPONSIBILITIES:
// - Register Infrastructure, the caller check and the endpoints
// - Order the middleware: headers, errors, disconnects, caller check
//
// IMPORTANT NOTES:
// - Internal only: never published by Edge (cross-plane rule 3)
// - Tests build the same pipeline through Build(args, configure)
// =========================================================
public static class OperationsApiHost
{
    public static WebApplication Build(string[] args)
    {
        return Build(args, null);
    }

    public static WebApplication Build(string[] args, Action<WebApplicationBuilder>? configure)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // The visitor's address and browser come from Customer.Web's headers (Slice 2); registered before the default.
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<Vixly.Operations.Application.Ports.IOperationsRequestMetadata, OperationsApiRequestMetadata>();
        builder.Services.AddOperationsInfrastructure(builder.Configuration, "Vixly.V4.Operations.Api");
        builder.Services.AddMemoryCache();
        builder.Services.Configure<OperationsInternalOptions>(builder.Configuration.GetSection(OperationsInternalOptions.SectionName));
        builder.Services.AddSingleton<OperationsCallerTokenValidator>();
        builder.Services.AddProblemDetails();

        configure?.Invoke(builder);

        WebApplication app = builder.Build();

        app.UseVixlySecurityHeaders();
        app.UseExceptionHandler(errors => errors.Run(context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Task.CompletedTask;
        }));
        app.UseVixlyClientDisconnects();
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseOperationsInternalAuthentication();

        app.MapVixlyHealth();
        app.MapOperationsInternalTenantEndpoints();
        app.MapOperationsInternalCustomerEndpoints();
        app.MapOperationsInternalPackageEndpoints();
        app.MapOperationsInternalTicketEndpoints();
        app.MapOperationsInternalInvoiceEndpoints();
        app.MapOperationsInternalShipmentEndpoints();
        app.MapOperationsInternalRateEndpoints();

        return app;
    }
}
