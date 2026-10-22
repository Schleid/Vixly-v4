using Microsoft.AspNetCore.Mvc;
using QRCoder;
using Vixly.Operations.Application.UseCases.Cashier.CancelOperationsCardPayment;
using Vixly.Operations.Application.UseCases.Cashier.ChangeOperationsCardAcceptance;
using Vixly.Operations.Application.UseCases.Cashier.ConnectOperationsCardProvider;
using Vixly.Operations.Application.UseCases.Cashier.DisconnectOperationsCardProvider;
using Vixly.Operations.Application.UseCases.Cashier.GetOperationsCardPayment;
using Vixly.Operations.Application.UseCases.Cashier.HandleOperationsCardWebhook;
using Vixly.Operations.Application.UseCases.Cashier.SaveOperationsCardReader;
using Vixly.Operations.Application.UseCases.Cashier.StartOperationsCardPayment;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Web.Components.Cashier;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Security;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsCardEndpoints.cs
//
// PURPOSE:
// Cards through the company's own Stripe account (Slice 5F step 4): Settings → Payments, the cashier's card payment, and Stripe's webhook.
//
// RESPONSIBILITIES:
// - /staff/settings/payments/card/*: connect, disconnect, cards on or off, readers
// - /staff/cashier/invoices/{id}/card and /staff/cashier/card/{chargeId}: start, follow (QR code, reader), cancel
// - POST /hooks/stripe: Stripe's events, trusted only when signed with the company's webhook secret
//
// IMPORTANT NOTES:
// - Static forms with antiforgery and the form guard (ADR-0019); the use cases check every permission
// - The webhook is anonymous: no antiforgery (Stripe signs it), at most 64 KB, rate-limited per host
// - The charge page refreshes itself every few seconds while it waits; it never decides "paid": Stripe does
// =========================================================
public static class OperationsCardEndpoints
{
    private const int MaxWebhookBytes = 64 * 1024;

    public static string WebhookUrl(HttpContext http) => "https://" + (http ?? throw new ArgumentNullException(nameof(http))).Request.Host.Value + "/hooks/stripe";

    public static IEndpointRouteBuilder MapOperationsCardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        staff.MapPost("/settings/payments/card/connect", async (HttpContext http, [FromForm] string? secretKey, [FromForm] string? publishableKey, [FromForm] string? webhookSecret,
            ConnectOperationsCardProviderUseCase connect) =>
        {
            ConnectOperationsCardProviderResponse response = await connect.ExecuteAsync(OperationsCurrentUser.Context(http), new ConnectOperationsCardProviderRequest
            {
                SecretKey = secretKey ?? string.Empty,
                PublishableKey = publishableKey ?? string.Empty,
                WebhookSecret = webhookSecret ?? string.Empty
            }, http.RequestAborted);
            return Back(response.Succeeded, response.Error, "connected");
        });

        staff.MapPost("/settings/payments/card/disconnect", async (HttpContext http, DisconnectOperationsCardProviderUseCase disconnect) =>
        {
            DisconnectOperationsCardProviderResponse response = await disconnect.ExecuteAsync(OperationsCurrentUser.Context(http), new DisconnectOperationsCardProviderRequest(), http.RequestAborted);
            return Back(response.Succeeded, response.Error, "disconnected");
        });

        staff.MapPost("/settings/payments/card/accept", async (HttpContext http, [FromForm] bool accept, ChangeOperationsCardAcceptanceUseCase change) =>
        {
            ChangeOperationsCardAcceptanceResponse response = await change.ExecuteAsync(OperationsCurrentUser.Context(http), new ChangeOperationsCardAcceptanceRequest { Accept = accept },
                http.RequestAborted);
            return Back(response.Succeeded, response.Error, "saved");
        });

        staff.MapPost("/settings/payments/card/readers", async (HttpContext http, [FromForm] Guid? remove, [FromForm] Guid? warehouse, [FromForm] string? label, [FromForm] string? reader,
            SaveOperationsCardReaderUseCase save) =>
        {
            SaveOperationsCardReaderResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), new SaveOperationsCardReaderRequest
            {
                RemoveReaderId = remove,
                WarehouseId = warehouse ?? Guid.Empty,
                Label = label,
                StripeReaderId = reader
            }, http.RequestAborted);
            return Back(response.Succeeded, response.Error, "saved");
        });

        // The cashier: Stripe's payment page (QR code) by default, or a reader of the invoice's warehouse.
        staff.MapPost("/cashier/invoices/{invoiceId:guid}/card", async (HttpContext http, Guid invoiceId, [FromForm] Guid? reader, StartOperationsCardPaymentUseCase start) =>
        {
            StartOperationsCardPaymentResponse response = await start.ExecuteAsync(OperationsCurrentUser.Context(http), new StartOperationsCardPaymentRequest
            {
                InvoiceId = invoiceId,
                Channel = reader is null ? OperationsCardChannel.PaymentPage : OperationsCardChannel.Reader,
                ReaderId = reader,
                ReturnUrlPrefix = "https://" + http.Request.Host.Value + "/staff/cashier/card/"
            }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/cashier/card/" + response.ChargeId)
                : Results.Redirect("/staff/cashier/invoices/" + invoiceId + "?card=" + Uri.EscapeDataString(response.Error?.Code ?? string.Empty));
        });

        staff.MapGet("/cashier/card/{chargeId:guid}", async (HttpContext http, Guid chargeId, GetOperationsCardPaymentUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
        {
            GetOperationsCardPaymentResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsCardPaymentRequest { ChargeId = chargeId }, http.RequestAborted);

            if (!response.Succeeded)
            {
                return Results.NotFound();
            }

            http.Response.Headers.CacheControl = "no-store";
            return OperationsStaffResults.Page<CardPaymentPage>(http, new
            {
                Company = await OperationsStaffResults.CompanyAsync(http, brand),
                Charge = response,
                QrDataUri = response.PageUrl is { } url ? QrDataUri(url) : null
            });
        });

        staff.MapPost("/cashier/card/{chargeId:guid}/cancel", async (HttpContext http, Guid chargeId, CancelOperationsCardPaymentUseCase cancel) =>
        {
            await cancel.ExecuteAsync(OperationsCurrentUser.Context(http), new CancelOperationsCardPaymentRequest { ChargeId = chargeId }, http.RequestAborted);
            return Results.Redirect("/staff/cashier/card/" + chargeId);
        });

        // Stripe's events: the body as Stripe sent it (the signature is over the exact bytes).
        endpoints.MapPost("/hooks/stripe", async (HttpContext http, HandleOperationsCardWebhookUseCase handle) =>
        {
            if (http.Request.ContentLength is > MaxWebhookBytes)
            {
                return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
            }

            using StreamReader reader = new(http.Request.Body, System.Text.Encoding.UTF8);
            char[] buffer = new char[MaxWebhookBytes + 1];
            int read = await reader.ReadBlockAsync(buffer.AsMemory(), http.RequestAborted);

            if (read > MaxWebhookBytes)
            {
                return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
            }

            HandleOperationsCardWebhookResponse response = await handle.ExecuteAsync(OperationsCurrentUser.Context(http), new HandleOperationsCardWebhookRequest
            {
                Payload = new string(buffer, 0, read),
                Signature = http.Request.Headers["Stripe-Signature"].ToString()
            }, http.RequestAborted);
            return response.Succeeded ? Results.Ok() : Results.BadRequest();
        }).AllowAnonymous().DisableAntiforgery().RequireRateLimiting(OperationsRateLimits.CardWebhook);

        return endpoints;
    }

    // Only a known refusal comes back through the address (never text typed in it).
    public static Vixly.Shared.Kernel.Errors.Error? Refusal(string? code) => new[]
    {
        OperationsCardErrors.KeysInvalid, OperationsCardErrors.NotConnected, OperationsCardErrors.ProviderRefused, OperationsCardErrors.ProviderUnavailable,
        OperationsCardErrors.ReaderInvalid, OperationsCardErrors.ChargeNotFound, OperationsCardErrors.AlreadyStarted, OperationsCardErrors.RefundNotPossible, OperationsCardErrors.PaymentsOpen,
        OperationsPaymentErrors.NothingOwed, Vixly.Operations.Domain.Pickup.OperationsPickupErrors.MoneyNotHandled,
        Vixly.Operations.Application.Security.OperationsAccessErrors.PermissionRequired
    }.FirstOrDefault(error => error.Code == code);

    private static IResult Back(bool succeeded, Vixly.Shared.Kernel.Errors.Error? error, string done) =>
        succeeded
            ? Results.Redirect("/staff/settings/payments?done=" + done + "#cards")
            : OperationsStaffEndpoints.StepUp(error, "/staff/settings/payments") ?? Results.Redirect("/staff/settings/payments?card=" + Uri.EscapeDataString(error?.Code ?? string.Empty) + "#cards");

    internal static string QrDataUri(string url)
    {
        using QRCodeGenerator generator = new();
        using QRCodeData data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        string svg = new SvgQRCode(data).GetGraphic(5);

        return "data:image/svg+xml;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(svg));
    }
}
