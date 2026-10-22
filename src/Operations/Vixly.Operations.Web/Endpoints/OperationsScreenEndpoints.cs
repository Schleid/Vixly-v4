using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Screens;
using Vixly.Operations.Application.UseCases.Screens.ActOnOperationsScreen;
using Vixly.Operations.Application.UseCases.Screens.ConnectOperationsCustomerScreen;
using Vixly.Operations.Application.UseCases.Screens.GetOperationsScreen;
using Vixly.Operations.Application.UseCases.Screens.GetOperationsScreenZelleQr;
using Vixly.Operations.Application.UseCases.Screens.ListOperationsCustomerScreens;
using Vixly.Operations.Application.UseCases.Screens.PairOperationsCustomerScreen;
using Vixly.Operations.Application.UseCases.Screens.RemoveOperationsCustomerScreen;
using Vixly.Operations.Application.UseCases.Screens.SendToOperationsCustomerScreen;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Screens;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Screens;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsScreenEndpoints.cs
//
// PURPOSE:
// Customer screens (Slice 5F step 3): the clerk's side (add, pair, send, clear) and the screen itself (/screen).
//
// RESPONSIBILITIES:
// - /staff/screens and the send forms on the cashier invoice and the counter shipment
// - /screen: the pairing code, then what the screen shows, the customer's choices, the version it polls, the Zelle QR picture
//
// IMPORTANT NOTES:
// - The screen has no sign-in: its own secret in a host-only, HttpOnly, Secure cookie; every form still has antiforgery and the form guard
// - The pairing code is shown only in the answer to the form that made it; codes typed on screens are rate-limited per address
// - Only a known refusal comes back through the address (never text typed in it)
// =========================================================
public static class OperationsScreenEndpoints
{
    private static readonly TimeSpan CookieLifetime = TimeSpan.FromDays(400);

    // As the use case: a drawn signature, never more.
    private const int SignatureMaxBytes = 200 * 1024;

    public static IEndpointRouteBuilder MapOperationsScreenEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        staff.MapGet("/screens", async (HttpContext http, ListOperationsCustomerScreensUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
            await ListAsync(http, list, brand, errors, null, null));

        staff.MapPost("/screens", async (HttpContext http, [FromForm] Guid? warehouse, [FromForm] string? name, PairOperationsCustomerScreenUseCase pair,
            ListOperationsCustomerScreensUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            PairOperationsCustomerScreenResponse response = await pair.ExecuteAsync(OperationsCurrentUser.Context(http),
                new PairOperationsCustomerScreenRequest { WarehouseId = warehouse ?? Guid.Empty, Name = name }, http.RequestAborted);
            return await ListAsync(http, list, brand, errors, response.Succeeded ? null : response.Error, response.Succeeded ? response : null, name);
        });

        staff.MapPost("/screens/{screenId:guid}/code", async (HttpContext http, Guid screenId, PairOperationsCustomerScreenUseCase pair, ListOperationsCustomerScreensUseCase list,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            PairOperationsCustomerScreenResponse response = await pair.ExecuteAsync(OperationsCurrentUser.Context(http), new PairOperationsCustomerScreenRequest { ScreenId = screenId },
                http.RequestAborted);
            return await ListAsync(http, list, brand, errors, response.Succeeded ? null : response.Error, response.Succeeded ? response : null);
        });

        staff.MapPost("/screens/{screenId:guid}/remove", async (HttpContext http, Guid screenId, RemoveOperationsCustomerScreenUseCase remove, ListOperationsCustomerScreensUseCase list,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            RemoveOperationsCustomerScreenResponse response = await remove.ExecuteAsync(OperationsCurrentUser.Context(http), new RemoveOperationsCustomerScreenRequest { ScreenId = screenId },
                http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/screens") : await ListAsync(http, list, brand, errors, response.Error, null);
        });

        // Sent from the cashier invoice or the counter shipment: back to that page.
        staff.MapPost("/screens/{screenId:guid}/send", async (HttpContext http, Guid screenId, [FromForm] Guid? invoice, [FromForm] Guid? shipment, [FromForm] bool? clear,
            [FromForm] string? back, SendToOperationsCustomerScreenUseCase send) =>
        {
            SendToOperationsCustomerScreenResponse response = await send.ExecuteAsync(OperationsCurrentUser.Context(http), new SendToOperationsCustomerScreenRequest
            {
                ScreenId = screenId,
                InvoiceId = clear == true ? null : invoice,
                ShipmentId = clear == true ? null : shipment,
                Clear = clear == true
            }, http.RequestAborted);
            string page = back switch
            {
                "invoice" when invoice is { } invoiceId => "/staff/cashier/invoices/" + invoiceId,
                "shipment" when shipment is { } shipmentId => "/staff/counter/shipments/" + shipmentId,
                _ => "/staff/screens"
            };
            return Results.Redirect(page + (response.Succeeded ? "?screen=sent" : "?screen=" + Uri.EscapeDataString(response.Error?.Code ?? string.Empty)) + "#screens");
        });

        RouteGroupBuilder screen = endpoints.MapGroup("/screen").AllowAnonymous().RequireFormToken();

        screen.MapGet("", async (HttpContext http, string? problem, string? from, GetOperationsScreenUseCase get, GetOperationsSignInBrandUseCase brand) =>
        {
            string? company = await OperationsStaffResults.CompanyAsync(http, brand);
            GetOperationsScreenResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsScreenRequest { Secret = Secret(http) },
                http.RequestAborted);

            if (!response.Succeeded)
            {
                // Back from Stripe on the customer's own phone (no screen cookie there): a thank-you, not the pairing form.
                return OperationsStaffResults.Page<ScreenPairPage>(http, new { Company = company, ProblemCode = Refusal(problem)?.Code, FromStripe = from == "stripe" });
            }

            return new OperationsCultureResult(OperationsStaffResults.Page<CustomerScreenPage>(http, new
            {
                Company = company,
                response.Step,
                response.Version,
                response.View,
                QrDataUri = response.View?.CardPageUrl is { } url ? OperationsCardEndpoints.QrDataUri(url) : null,
                ProblemCode = Refusal(problem)?.Code
            }), response.Language);
        });

        screen.MapPost("/pair", async (HttpContext http, [FromForm] string? code, ConnectOperationsCustomerScreenUseCase connect) =>
        {
            ConnectOperationsCustomerScreenResponse response = await connect.ExecuteAsync(OperationsCurrentUser.Context(http), new ConnectOperationsCustomerScreenRequest { Code = code },
                http.RequestAborted);

            if (!response.Succeeded)
            {
                return Results.Redirect("/screen?problem=" + Uri.EscapeDataString(response.Error?.Code ?? string.Empty));
            }

            OperationsCookies.Write(http.Response, OperationsCookies.Screen, response.Secret, CookieLifetime);
            return Results.Redirect("/screen");
        }).RequireRateLimiting(OperationsRateLimits.ScreenPair);

        screen.MapGet("/version", async (HttpContext http, GetOperationsScreenUseCase get) =>
        {
            GetOperationsScreenResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsScreenRequest { Secret = Secret(http), VersionOnly = true },
                http.RequestAborted);
            http.Response.Headers.CacheControl = "no-store";
            return response.Succeeded ? Results.Text(response.Version, "text/plain") : Results.Unauthorized();
        });

        screen.MapPost("/act", async (HttpContext http, ActOnOperationsScreenUseCase act) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            byte[]? signature = null;
            string drawn = form["signature"].ToString();

            if (drawn.StartsWith("data:image/png;base64,", StringComparison.Ordinal) && drawn.Length <= SignatureMaxBytes * 4 / 3 + 64)
            {
                try
                {
                    signature = Convert.FromBase64String(drawn["data:image/png;base64,".Length..]);
                }
                catch (FormatException)
                {
                    signature = null;
                }
            }

            ActOnOperationsScreenResponse response = await act.ExecuteAsync(OperationsCurrentUser.Context(http), new ActOnOperationsScreenRequest
            {
                Secret = Secret(http),
                Action = Enum.TryParse(form["action"].ToString(), false, out OperationsScreenAction action) && Enum.IsDefined(action) ? action : OperationsScreenAction.None,
                Version = form["version"].ToString(),
                Signature = signature,
                Accepted = form["accept"].ToString() == "true",
                ReturnUrl = "https://" + http.Request.Host.Value + "/screen?from=stripe"
            }, http.RequestAborted);
            return Results.Redirect(response.Succeeded ? "/screen" : "/screen?problem=" + Uri.EscapeDataString(response.Error?.Code ?? string.Empty));
        });

        screen.MapGet("/zelle-qr", async (HttpContext http, GetOperationsScreenZelleQrUseCase picture) =>
        {
            GetOperationsScreenZelleQrResponse response = await picture.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsScreenZelleQrRequest { Secret = Secret(http) },
                http.RequestAborted);

            if (!response.Succeeded)
            {
                return Results.NotFound();
            }

            http.Response.Headers.CacheControl = "private, no-store";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.File(response.Content, response.ContentType);
        });

        return endpoints;
    }

    // The screens of one warehouse, for the send panel on a cashier invoice or a counter shipment.
    public static async Task<IReadOnlyList<OperationsScreenRow>> ScreensAtAsync(HttpContext http, ListOperationsCustomerScreensUseCase list, Guid? warehouseId)
    {
        ArgumentNullException.ThrowIfNull(list);

        if (warehouseId is null)
        {
            return Array.Empty<OperationsScreenRow>();
        }

        ListOperationsCustomerScreensResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsCustomerScreensRequest(), http.RequestAborted);
        return response.Succeeded ? response.Screens.Where(screen => screen.WarehouseId == warehouseId).ToList() : Array.Empty<OperationsScreenRow>();
    }

    // Only a known refusal comes back through the address.
    public static Error? Refusal(string? code) => new[]
    {
        OperationsScreenErrors.NotFound, OperationsScreenErrors.StepInvalid, OperationsScreenErrors.NotPaired, OperationsScreenErrors.CodeInvalid,
        OperationsScreenErrors.SignatureRequired, OperationsScreenErrors.AcceptRequired, OperationsScreenErrors.NotOffered, OperationsCardErrors.ProviderUnavailable, OperationsCardErrors.ProviderRefused,
        OperationsCardErrors.AlreadyStarted, OperationsCardErrors.NotConnected, OperationsPaymentErrors.NothingOwed, OperationsCommonErrors.ConcurrencyConflict,
        Vixly.Operations.Domain.Receiving.OperationsReceivingErrors.ShipmentNotOpen
    }.FirstOrDefault(error => error.Code == code);

    private static string? Secret(HttpContext http) => http.Request.Cookies[OperationsCookies.Screen];

    private static async Task<IResult> ListAsync(HttpContext http, ListOperationsCustomerScreensUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors, Error? error,
        PairOperationsCustomerScreenResponse? paired, string? name = null)
    {
        ListOperationsCustomerScreensResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsCustomerScreensRequest(), http.RequestAborted);
        return OperationsStaffResults.Page<ScreensPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Screens = response.Screens,
            Warehouses = response.Warehouses,
            ScreenUrl = "https://" + http.Request.Host.Value + "/screen",
            Code = paired?.Code,
            CodeFor = paired is null ? null : response.Screens.FirstOrDefault(screen => screen.ScreenId == paired.ScreenId)?.Name ?? name,
            CodeExpiresUtc = paired?.ExpiresUtc,
            Error = errors.Describe(error ?? response.Error)
        });
    }
}
