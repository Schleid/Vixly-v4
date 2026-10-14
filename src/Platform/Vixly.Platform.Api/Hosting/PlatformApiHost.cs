using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using Vixly.Platform.Api.Endpoints;
using Vixly.Platform.Application.DependencyInjection;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Infrastructure.DependencyInjection;
using Vixly.Shared.Hosting;
using Vixly.Shared.Localization;

namespace Vixly.Platform.Api.Hosting;

// =========================================================
// FILE: PlatformApiHost.cs
//
// PURPOSE:
// Composition root of Vixly.Platform.Api (opens in Slice 2).
//
// RESPONSIBILITIES:
// - Register Application and Infrastructure
// - Apply forwarded headers, security headers, body limit, CORS,
//   request localisation and rate limits
// - Map health and the public registration endpoints
//
// IMPORTANT NOTES:
// - No cookies and no sessions: the API is stateless
// - Internal endpoints for Edge and Operations (JWT) come in
//   Slice 9
// - Errors never carry exception details (S-9)
// =========================================================
public static class PlatformApiHost
{
    public const string CorsPolicy = "public-site";

    public static WebApplication Build(string[] args)
    {
        return Build(args, null);
    }

    public static WebApplication Build(string[] args, Action<WebApplicationBuilder>? configure)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        PlatformApiOptions api = builder.Configuration.GetSection(PlatformApiOptions.SectionName).Get<PlatformApiOptions>() ?? new PlatformApiOptions();

        builder.Services.AddPlatformInfrastructure(builder.Configuration, "Vixly.V4.Platform.Api");
        builder.Services.AddPlatformApplication();
        builder.Services.AddVixlyForwardedHeaders(builder.Configuration);
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IPlatformRequestMetadata, PlatformApiRequestMetadata>();
        builder.Services.AddScoped<PlatformApiErrors>();
        builder.Services.AddPlatformApiRateLimits();
        builder.Services.AddProblemDetails();
        builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
            .WithOrigins(api.AllowedOrigins)
            .WithMethods("GET", "POST")
            .WithHeaders("Content-Type", "Accept-Language")));

        configure?.Invoke(builder);

        WebApplication app = builder.Build();

        app.UseForwardedHeaders();
        app.UseVixlySecurityHeaders();
        app.UseExceptionHandler(errors => errors.Run(context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            return Task.CompletedTask;
        }));
        app.Use((context, next) =>
        {
            if (context.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } limit)
            {
                limit.MaxRequestBodySize = api.MaxRequestBodyBytes;
            }

            if (context.Request.ContentLength > api.MaxRequestBodyBytes)
            {
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                return Task.CompletedTask;
            }

            return next(context);
        });
        app.UseHttpsRedirection();
        app.UseRequestLocalization(options =>
        {
            options.DefaultRequestCulture = new RequestCulture(VixlyCultures.Default);
            options.SupportedCultures = VixlyCultures.Supported.ToList();
            options.SupportedUICultures = VixlyCultures.Supported.ToList();
            options.RequestCultureProviders = new List<IRequestCultureProvider> { new AcceptLanguageHeaderRequestCultureProvider() };
        });
        app.UseCors();
        app.UseRateLimiter();

        app.MapVixlyHealth();
        app.MapPlatformRegistrationEndpoints();

        return app;
    }
}
