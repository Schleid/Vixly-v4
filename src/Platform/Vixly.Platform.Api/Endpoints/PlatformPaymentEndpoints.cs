using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vixly.Contracts.Platform.Payments;
using Vixly.Platform.Api.Hosting;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Application.UseCases.Payments.GetPlatformZelleQrCode;
using Vixly.Platform.Application.UseCases.Payments.GetRegistrationInvoice;
using Vixly.Platform.Application.UseCases.Payments.OpenPlatformPaymentLink;
using Vixly.Platform.Application.UseCases.Payments.ReceivePlatformProviderEvent;
using Vixly.Platform.Application.UseCases.Payments.ReportPlatformZellePayment;
using Vixly.Platform.Application.UseCases.Payments.ReportRegistrationZellePayment;
using Vixly.Platform.Application.UseCases.Payments.StartPlatformLinkCheckout;
using Vixly.Platform.Application.UseCases.Payments.StartRegistrationCheckout;
using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Api.Endpoints;

// =========================================================
// FILE: PlatformPaymentEndpoints.cs
//
// PURPOSE:
// Card-payment endpoints of Platform.Api (Slice 5, design section 6).
//
// RESPONSIBILITIES:
// - Receive the provider webhook
// - Show a pay or save-card link and open the provider's checkout from it
// - Show the registration invoice and open its checkout
//
// IMPORTANT NOTES:
// - Anonymous (S-6): each endpoint has a rate-limit policy; the webhook needs a valid signature
// - Answers are never cached; amounts always come from the server
// =========================================================
public static class PlatformPaymentEndpoints
{
    private static readonly PlatformAnonymousAuthorizationContext Anonymous = PlatformAnonymousAuthorizationContext.Instance;

    public static IEndpointRouteBuilder MapPlatformPaymentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/payments/webhook", async (
            HttpContext http,
            ReceivePlatformProviderEventUseCase useCase,
            PlatformApiErrors errors,
            CancellationToken cancellationToken) =>
        {
            using StreamReader reader = new(http.Request.Body);
            string payload = await reader.ReadToEndAsync(cancellationToken);
            string? signature = http.Request.Headers["Stripe-Signature"].FirstOrDefault()
                ?? http.Request.Headers[Vixly.Platform.Infrastructure.Payments.SimulatedCardGateway.SignatureHeader].FirstOrDefault();
            ReceivePlatformProviderEventResponse response = await useCase.ExecuteAsync(
                Anonymous,
                new ReceivePlatformProviderEventRequest { Payload = payload, Signature = signature },
                cancellationToken);

            return response.Succeeded ? Results.Ok() : errors.Problem(response.Error!);
        }).RequireRateLimiting(PlatformApiRateLimits.Webhook).DisableAntiforgery();

        RouteGroupBuilder links = endpoints.MapGroup("/payment-links").AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            return await next(context);
        });

        links.MapGet("/{token}", async (
            string token,
            OpenPlatformPaymentLinkUseCase useCase,
            PlatformApiErrors errors,
            CancellationToken cancellationToken) =>
        {
            OpenPlatformPaymentLinkResponse response = await useCase.ExecuteAsync(Anonymous, new OpenPlatformPaymentLinkRequest { Token = token }, cancellationToken);
            return response.Succeeded ? Results.Ok(Info(response.Link!)) : errors.Problem(response.Error!);
        }).RequireRateLimiting(PlatformApiRateLimits.PaymentLinkView);

        links.MapPost("/{token}/checkout", async (
            string token,
            PlatformCheckoutBody body,
            StartPlatformLinkCheckoutUseCase useCase,
            PlatformApiErrors errors,
            CancellationToken cancellationToken) =>
        {
            StartPlatformLinkCheckoutResponse response = await useCase.ExecuteAsync(
                Anonymous,
                new StartPlatformLinkCheckoutRequest { Token = token, SaveCard = body.SaveCard, AutoCharge = body.AutoCharge },
                cancellationToken);
            return response.Succeeded ? Results.Ok(new PlatformCheckoutStarted(response.CheckoutUrl)) : errors.Problem(response.Error!);
        }).RequireRateLimiting(PlatformApiRateLimits.PaymentCheckout);

        // The customer reports a Zelle payment; staff check it before the invoice is paid.
        links.MapPost("/{token}/zelle", async (
            string token,
            PlatformZelleReportBody body,
            ReportPlatformZellePaymentUseCase useCase,
            PlatformApiErrors errors,
            CancellationToken cancellationToken) =>
        {
            ReportPlatformZellePaymentResponse response = await useCase.ExecuteAsync(
                Anonymous,
                new ReportPlatformZellePaymentRequest { Token = token, Amount = body.Amount, SentOn = body.SentOn, SenderName = body.SenderName, Reference = body.Reference },
                cancellationToken);
            return response.Succeeded ? Results.NoContent() : errors.Problem(response.Error!);
        }).RequireRateLimiting(PlatformApiRateLimits.PaymentCheckout);

        endpoints.MapGet("/payments/zelle/qr", async (
            HttpContext http,
            GetPlatformZelleQrCodeUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            GetPlatformZelleQrCodeResponse response = await useCase.ExecuteAsync(Anonymous, new GetPlatformZelleQrCodeRequest(), cancellationToken);

            if (!response.Succeeded)
            {
                return Results.NotFound();
            }

            // The URL carries the version, so a new image is a new URL.
            http.Response.Headers.CacheControl = "public, max-age=3600";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(response.QrCode!.Content, response.QrCode.ContentType);
        }).RequireRateLimiting(PlatformApiRateLimits.PaymentLinkView);

        endpoints.MapGet("/registration/invoice", async (
            HttpContext http,
            GetRegistrationInvoiceUseCase useCase,
            PlatformApiErrors errors,
            CancellationToken cancellationToken) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            GetRegistrationInvoiceResponse response = await useCase.ExecuteAsync(Anonymous, new GetRegistrationInvoiceRequest { AccessToken = Bearer(http) }, cancellationToken);
            return response.Succeeded ? Results.Ok(Info(response.Invoice!)) : errors.Problem(response.Error!);
        }).RequireRateLimiting(PlatformApiRateLimits.Status);

        endpoints.MapPost("/registration/checkout", async (
            HttpContext http,
            PlatformCheckoutBody body,
            StartRegistrationCheckoutUseCase useCase,
            PlatformApiErrors errors,
            CancellationToken cancellationToken) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            StartRegistrationCheckoutResponse response = await useCase.ExecuteAsync(
                Anonymous,
                new StartRegistrationCheckoutRequest { AccessToken = Bearer(http), SaveCard = body.SaveCard, AutoCharge = body.AutoCharge },
                cancellationToken);
            return response.Succeeded ? Results.Ok(new PlatformCheckoutStarted(response.CheckoutUrl)) : errors.Problem(response.Error!);
        }).RequireRateLimiting(PlatformApiRateLimits.PaymentCheckout);

        endpoints.MapPost("/registration/zelle", async (
            HttpContext http,
            PlatformZelleReportBody body,
            ReportRegistrationZellePaymentUseCase useCase,
            PlatformApiErrors errors,
            CancellationToken cancellationToken) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            ReportRegistrationZellePaymentResponse response = await useCase.ExecuteAsync(
                Anonymous,
                new ReportRegistrationZellePaymentRequest { AccessToken = Bearer(http), Amount = body.Amount, SentOn = body.SentOn, SenderName = body.SenderName, Reference = body.Reference },
                cancellationToken);
            return response.Succeeded ? Results.NoContent() : errors.Problem(response.Error!);
        }).RequireRateLimiting(PlatformApiRateLimits.PaymentCheckout);

        return endpoints;
    }

    private static PlatformPaymentLinkInfo Info(PlatformPaymentLinkSummary summary)
    {
        return new PlatformPaymentLinkInfo(
            summary.Kind == PlatformPaymentLinkKind.SaveCard ? "save_card" : "pay_invoice",
            summary.TenantName,
            summary.InvoiceNumber,
            summary.Balance,
            summary.CurrencyCode,
            summary.DueUtc,
            summary.Settled,
            summary.CardPaymentsAvailable,
            summary.ConsentTextVersion,
            summary.Zelle is { } zelle
                ? new PlatformZelleInfo(
                    zelle.RecipientName,
                    zelle.Email,
                    zelle.Tag,
                    zelle.Phone,
                    zelle.QrCodeVersion is int version ? "/payments/zelle/qr?v=" + version.ToString(System.Globalization.CultureInfo.InvariantCulture) : null,
                    zelle.Waiting is { } waiting ? new PlatformZelleReportInfo(waiting.Amount, waiting.SentOn, waiting.ReportedUtc) : null,
                    zelle.CanReport)
                : null);
    }

    private static string Bearer(HttpContext http)
    {
        string header = http.Request.Headers.Authorization.ToString();

        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header["Bearer ".Length..].Trim() : string.Empty;
    }
}
