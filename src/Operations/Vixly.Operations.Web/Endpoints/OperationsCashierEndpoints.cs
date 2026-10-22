using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.UseCases.Cashier.GetOperationsCashier;
using Vixly.Operations.Application.UseCases.Cashier.GetOperationsCashierInvoice;
using Vixly.Operations.Application.UseCases.Cashier.TakeOperationsPayment;
using Vixly.Operations.Application.UseCases.Cashier.GetOperationsPayment;
using Vixly.Operations.Application.UseCases.Cashier.GetOperationsPaymentReceipt;
using Vixly.Operations.Application.UseCases.Cashier.OpenOperationsCashDrawer;
using Vixly.Operations.Application.UseCases.Cashier.CloseOperationsCashDrawer;
using Vixly.Operations.Application.UseCases.Cashier.ListOperationsCashDrawers;
using Vixly.Operations.Application.UseCases.Cashier.GetOperationsCashDrawer;
using Vixly.Operations.Application.UseCases.Cashier.ListOperationsPaymentsToVerify;
using Vixly.Operations.Application.UseCases.Cashier.CheckOperationsPayment;
using Vixly.Operations.Application.UseCases.Cashier.RefundOperationsPayment;
using Vixly.Operations.Application.UseCases.Cashier.GetOperationsPaymentSettings;
using Vixly.Operations.Application.UseCases.Cashier.SaveOperationsPaymentSettings;
using Vixly.Operations.Application.UseCases.Cashier.GetOperationsZelleQr;
using Vixly.Operations.Application.UseCases.Cashier.BillOperationsAccount;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Cashier;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsCashierEndpoints.cs
//
// PURPOSE:
// The cashier (Slice 5F step 2): find and pay, the receipt, cash drawers, payments to verify, refunds, and Settings → Payments.
//
// RESPONSIBILITIES:
// - /staff/cashier and its forms; /staff/invoices/{id}/refunds; /staff/settings/payments
//
// IMPORTANT NOTES:
// - Static forms with antiforgery and the form guard (ADR-0019)
// - The use cases check every permission
// =========================================================
public static class OperationsCashierEndpoints
{
    private const int MaxPictureBytes = 1024 * 1024;

    public static IEndpointRouteBuilder MapOperationsCashierEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        staff.MapGet("/cashier", async (HttpContext http, string? find, Guid? customer, string? done, GetOperationsCashierUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> texts) =>
        {
            string? text = find is { Length: > 0 and <= 80 } ? find.Trim() : null;
            GetOperationsCashierResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsCashierRequest { Find = text, CustomerId = customer },
                http.RequestAborted);
            return OperationsStaffResults.Page<CashierPage>(http, new
            {
                Company = await OperationsStaffResults.CompanyAsync(http, brand),
                Find = text,
                Matches = response.Matches,
                Customer = response.Customer,
                Invoices = response.Invoices,
                Drawers = response.Drawers,
                Warehouses = response.Warehouses,
                Error = errors.Describe(response.Error),
                Info = done == "opened" ? texts["ui.drawers.done.opened"].Value : null
            });
        });

        staff.MapGet("/cashier/invoices/{invoiceId:guid}", async (HttpContext http, Guid invoiceId, string? done, string? card, string? screen, GetOperationsCashierInvoiceUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors, IStringLocalizer<OperationsText> texts) =>
            await InvoiceAsync(http, invoiceId, OperationsCardEndpoints.Refusal(card) ?? OperationsScreenEndpoints.Refusal(screen), get, brand, errors,
                screen == "sent" ? texts["ui.screens.sent"].Value : done is "billed" or "awaiting_credit" ? texts["ui.credit.done." + done].Value : null));

        // Slice 5F step 2b (K-3): billed to the customer's credit account.
        staff.MapPost("/cashier/invoices/{invoiceId:guid}/bill-account", async (HttpContext http, Guid invoiceId, BillOperationsAccountUseCase bill, GetOperationsCashierInvoiceUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            BillOperationsAccountResponse response = await bill.ExecuteAsync(OperationsCurrentUser.Context(http), new BillOperationsAccountRequest { InvoiceId = invoiceId }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/cashier/invoices/" + invoiceId + "?done=" + (response.Awaiting ? "awaiting_credit" : "billed"))
                : await InvoiceAsync(http, invoiceId, response.Error, get, brand, errors);
        });

        // Paid in full with up to three tenders: the method, the amount, the cash handed over, the reference.
        staff.MapPost("/cashier/invoices/{invoiceId:guid}/pay", async (HttpContext http, Guid invoiceId, TakeOperationsPaymentUseCase take, GetOperationsCashierInvoiceUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            List<OperationsTenderDraft> tenders = new();

            for (int row = 1; row <= 3; row++)
            {
                string amountText = form["amount_" + row].ToString();

                if (string.IsNullOrWhiteSpace(amountText))
                {
                    continue;
                }

                string givenText = form["given_" + row].ToString();

                if (!Enum.TryParse(form["method_" + row], false, out OperationsPaymentMethod method) || OperationsCounterEndpoints.Amount(amountText) is not { } amount
                    || (!string.IsNullOrWhiteSpace(givenText) && OperationsCounterEndpoints.Amount(givenText) is null))
                {
                    return await InvoiceAsync(http, invoiceId, OperationsPaymentErrors.AmountInvalid, get, brand, errors);
                }

                tenders.Add(new OperationsTenderDraft(method, amount, string.IsNullOrWhiteSpace(givenText) ? null : OperationsCounterEndpoints.Amount(givenText), form["reference_" + row]));
            }

            TakeOperationsPaymentResponse response = await take.ExecuteAsync(OperationsCurrentUser.Context(http), new TakeOperationsPaymentRequest
            {
                InvoiceId = invoiceId,
                Tenders = tenders,
                ZelleTermsAccepted = form["zelleTerms"] == "true",
                RequestKey = Guid.TryParseExact(form["key"], "N", out Guid key) ? key : Guid.Empty
            }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/cashier/payments/" + response.PaymentId)
                : await InvoiceAsync(http, invoiceId, response.Error, get, brand, errors);
        });

        staff.MapGet("/cashier/payments/{paymentId:guid}", async (HttpContext http, Guid paymentId, GetOperationsPaymentUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
        {
            GetOperationsPaymentResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsPaymentRequest { PaymentId = paymentId }, http.RequestAborted);
            return OperationsStaffResults.Page<CashierPaymentPage>(http, new
            {
                Company = await OperationsStaffResults.CompanyAsync(http, brand),
                Payment = response.Succeeded ? response.Payment : null,
                Invoice = response.Succeeded ? response.Invoice : null,
                OtherNames = response.OtherNames,
                Error = errors.Describe(response.Error)
            });
        });

        staff.MapGet("/cashier/payments/{paymentId:guid}/receipt", async (HttpContext http, Guid paymentId, GetOperationsPaymentReceiptUseCase receipt) =>
        {
            GetOperationsPaymentReceiptResponse response = await receipt.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsPaymentReceiptRequest { PaymentId = paymentId },
                http.RequestAborted);
            return response.Succeeded ? OperationsStaffResults.File(http, response.Content, "application/pdf", response.FileName) : Results.NotFound();
        });

        staff.MapGet("/cashier/drawers", async (HttpContext http, string? done, ListOperationsCashDrawersUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors,
            IStringLocalizer<OperationsText> text) =>
            await DrawersAsync(http, null, done is "opened" or "closed" ? text["ui.drawers.done." + done].Value : null, list, brand, errors));

        staff.MapPost("/cashier/drawers/open", async (HttpContext http, [FromForm] Guid? warehouse, [FromForm(Name = "float")] string? startingFloat, OpenOperationsCashDrawerUseCase open,
            ListOperationsCashDrawersUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            if (OperationsCounterEndpoints.Amount(startingFloat) is not { } amount)
            {
                return await DrawersAsync(http, OperationsPaymentErrors.AmountInvalid, null, list, brand, errors);
            }

            OpenOperationsCashDrawerResponse response = await open.ExecuteAsync(OperationsCurrentUser.Context(http),
                new OpenOperationsCashDrawerRequest { WarehouseId = warehouse ?? Guid.Empty, Float = amount }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/cashier?done=opened") : await DrawersAsync(http, response.Error, null, list, brand, errors);
        });

        staff.MapGet("/cashier/drawers/{drawerId:guid}", async (HttpContext http, Guid drawerId, GetOperationsCashDrawerUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
            await DrawerAsync(http, drawerId, null, get, brand, errors));

        staff.MapPost("/cashier/drawers/{drawerId:guid}/close", async (HttpContext http, Guid drawerId, [FromForm] string? counted, [FromForm] string? note, CloseOperationsCashDrawerUseCase close,
            GetOperationsCashDrawerUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            if (OperationsCounterEndpoints.Amount(counted) is not { } amount)
            {
                return await DrawerAsync(http, drawerId, OperationsPaymentErrors.AmountInvalid, get, brand, errors);
            }

            CloseOperationsCashDrawerResponse response = await close.ExecuteAsync(OperationsCurrentUser.Context(http),
                new CloseOperationsCashDrawerRequest { DrawerId = drawerId, Counted = amount, Note = note }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/cashier/drawers?done=closed") : await DrawerAsync(http, drawerId, response.Error, get, brand, errors);
        });

        staff.MapGet("/cashier/verify", async (HttpContext http, string? done, ListOperationsPaymentsToVerifyUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors,
            IStringLocalizer<OperationsText> text) =>
            await VerifyAsync(http, null, done is "received" or "not_received" ? text["ui.payments_verify.done." + done].Value : null, list, brand, errors));

        staff.MapPost("/cashier/verify/{paymentId:guid}/{tenderId:guid}", async (HttpContext http, Guid paymentId, Guid tenderId, [FromForm] string? received, [FromForm] string? note,
            CheckOperationsPaymentUseCase check, ListOperationsPaymentsToVerifyUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            bool yes = received == "true";
            CheckOperationsPaymentResponse response = await check.ExecuteAsync(OperationsCurrentUser.Context(http), new CheckOperationsPaymentRequest
            {
                PaymentId = paymentId, TenderId = tenderId, Received = yes, Note = note, PortalSlug = OperationsStaffResults.PortalSlug(http)
            }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/cashier/verify?done=" + (yes ? "received" : "not_received"))
                : await VerifyAsync(http, response.Error, null, list, brand, errors);
        });

        staff.MapPost("/invoices/{invoiceId:guid}/refunds", async (HttpContext http, Guid invoiceId, [FromForm] string? method, [FromForm] string? amount, [FromForm] string? reason,
            [FromForm] string? reference, [FromForm] string? key, RefundOperationsPaymentUseCase refund) =>
        {
            RefundOperationsPaymentResponse response = await refund.ExecuteAsync(OperationsCurrentUser.Context(http), new RefundOperationsPaymentRequest
            {
                InvoiceId = invoiceId,
                Method = Enum.TryParse(method, false, out OperationsPaymentMethod chosen) ? chosen : 0,
                Amount = OperationsCounterEndpoints.Amount(amount) ?? -1m,
                Reason = reason,
                Reference = reference,
                RequestKey = Guid.TryParseExact(key, "N", out Guid parsed) ? parsed : Guid.Empty
            }, http.RequestAborted);
            return Results.Redirect("/staff/invoices/" + invoiceId + "?done=" + (response.Succeeded ? "refunded" : "refund_refused&why=" + Uri.EscapeDataString(response.Error?.Code ?? string.Empty)));
        });

        staff.MapGet("/settings/payments", async (HttpContext http, string? done, string? card, GetOperationsPaymentSettingsUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
            await SettingsAsync(http, OperationsCardEndpoints.Refusal(card), done is null ? null : text["ui.staff.done.saved"].Value, get, brand, errors));

        staff.MapPost("/settings/payments", async (HttpContext http, SaveOperationsPaymentSettingsUseCase save, GetOperationsPaymentSettingsUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
        {
            IFormCollection form;

            try
            {
                form = await http.Request.ReadFormAsync(http.RequestAborted);
            }
            catch (Exception exception) when (exception is InvalidDataException or BadHttpRequestException)
            {
                return await SettingsAsync(http, OperationsPaymentErrors.PictureInvalid, null, get, brand, errors);
            }

            byte[]? picture = null;

            if (form.Files.GetFile("qr") is { Length: > 0 } file)
            {
                if (file.Length > MaxPictureBytes)
                {
                    return await SettingsAsync(http, OperationsPaymentErrors.PictureInvalid, null, get, brand, errors);
                }

                using MemoryStream buffer = new((int)file.Length);
                await file.CopyToAsync(buffer, http.RequestAborted);
                picture = buffer.ToArray();
            }

            SaveOperationsPaymentSettingsResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), new SaveOperationsPaymentSettingsRequest
            {
                Cash = form["cash"] == "true",
                Zelle = form["zelle"] == "true",
                Cheque = form["cheque"] == "true",
                BankTransfer = form["transfer"] == "true",
                Other1 = form["other1"],
                Other2 = form["other2"],
                Other3 = form["other3"],
                ZelleTag = form["zelleTag"],
                ZelleContact = form["zelleContact"],
                ZelleName = form["zelleName"],
                ZelleTermsEn = form["termsEn"],
                ZelleTermsFr = form["termsFr"],
                ZelleTermsEs = form["termsEs"],
                ZelleQr = picture,
                RemoveZelleQr = form["removeQr"] == "true"
            }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/settings/payments?done=1")
                : OperationsStaffEndpoints.StepUp(response.Error, "/staff/settings/payments") ?? await SettingsAsync(http, response.Error, null, get, brand, errors);
        }).WithFormOptions(multipartBodyLengthLimit: MaxPictureBytes + (64 * 1024));

        staff.MapGet("/settings/payments/zelle-qr", async (HttpContext http, GetOperationsZelleQrUseCase picture) =>
        {
            GetOperationsZelleQrResponse response = await picture.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsZelleQrRequest(), http.RequestAborted);

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

    private static async Task<IResult> InvoiceAsync(HttpContext http, Guid invoiceId, Error? error, GetOperationsCashierInvoiceUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors, string? info = null)
    {
        GetOperationsCashierInvoiceResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsCashierInvoiceRequest { InvoiceId = invoiceId },
            http.RequestAborted);
        return OperationsStaffResults.Page<CashierInvoicePage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Invoice = response.Succeeded ? response.Invoice : null,
            Settings = response.Succeeded ? response.Settings : null,
            Methods = response.Methods,
            CardOffered = response.CardOffered,
            CardReaders = response.CardReaders,
            Drawer = response.Drawer,
            Payments = response.Payments,
            Refunds = response.Refunds,
            Account = response.Account,
            Position = response.Position,
            Screens = await OperationsScreenEndpoints.ScreensAtAsync(http,
                http.RequestServices.GetRequiredService<Vixly.Operations.Application.UseCases.Screens.ListOperationsCustomerScreens.ListOperationsCustomerScreensUseCase>(),
                response.Succeeded ? response.Invoice?.WarehouseId : null),
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }

    private static async Task<IResult> DrawersAsync(HttpContext http, Error? error, string? info, ListOperationsCashDrawersUseCase list, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        ListOperationsCashDrawersResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsCashDrawersRequest(), http.RequestAborted);
        return OperationsStaffResults.Page<CashDrawersPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Drawers = response.Drawers,
            Warehouses = response.Warehouses,
            People = response.People,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }

    private static async Task<IResult> DrawerAsync(HttpContext http, Guid drawerId, Error? error, GetOperationsCashDrawerUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsCashDrawerResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsCashDrawerRequest { DrawerId = drawerId }, http.RequestAborted);
        return OperationsStaffResults.Page<CashDrawerPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Drawer = response.Succeeded ? response.Drawer : null,
            Payments = response.Payments,
            Refunds = response.Refunds,
            Person = response.Person,
            Own = response.Own,
            Error = errors.Describe(error ?? response.Error)
        });
    }

    private static async Task<IResult> VerifyAsync(HttpContext http, Error? error, string? info, ListOperationsPaymentsToVerifyUseCase list, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        ListOperationsPaymentsToVerifyResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsPaymentsToVerifyRequest(), http.RequestAborted);
        return OperationsStaffResults.Page<VerifyPaymentsPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Payments = response.Payments,
            Invoices = response.Invoices,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }

    private static async Task<IResult> SettingsAsync(HttpContext http, Error? error, string? info, GetOperationsPaymentSettingsUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsPaymentSettingsResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsPaymentSettingsRequest(), http.RequestAborted);
        Vixly.Operations.Application.UseCases.Cashier.GetOperationsCardSettings.GetOperationsCardSettingsResponse card = await http.RequestServices
            .GetRequiredService<Vixly.Operations.Application.UseCases.Cashier.GetOperationsCardSettings.GetOperationsCardSettingsUseCase>()
            .ExecuteAsync(OperationsCurrentUser.Context(http), new Vixly.Operations.Application.UseCases.Cashier.GetOperationsCardSettings.GetOperationsCardSettingsRequest(), http.RequestAborted);
        return OperationsStaffResults.Page<PaymentSettingsPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Settings = response.Succeeded ? response.Settings : null,
            CanEdit = response.CanEdit,
            Card = card,
            WebhookUrl = OperationsCardEndpoints.WebhookUrl(http),
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }
}
