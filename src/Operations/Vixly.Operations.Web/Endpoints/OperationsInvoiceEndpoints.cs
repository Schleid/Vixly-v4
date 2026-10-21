using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.UseCases.Billing.DecideOperationsCredit;
using Vixly.Operations.Application.UseCases.Billing.GetOperationsInvoice;
using Vixly.Operations.Application.UseCases.Billing.GetOperationsInvoiceDocument;
using Vixly.Operations.Application.UseCases.Billing.GetOperationsInvoiceSettings;
using Vixly.Operations.Application.UseCases.Billing.ListOperationsCreditRequests;
using Vixly.Operations.Application.UseCases.Billing.ListOperationsInvoices;
using Vixly.Operations.Application.UseCases.Billing.RequestOperationsCredit;
using Vixly.Operations.Application.UseCases.Billing.SaveOperationsInvoiceSettings;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Invoices;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsInvoiceEndpoints.cs
//
// PURPOSE:
// Invoices (Slice 5F step 1): the list, an invoice and its PDF, credit notes asked and decided, the requests to approve, and Settings → Invoices.
//
// RESPONSIBILITIES:
// - /staff/invoices and its forms; /staff/settings/invoices
//
// IMPORTANT NOTES:
// - Static forms with antiforgery and the form guard (ADR-0019)
// - The use cases check every permission
// =========================================================
public static class OperationsInvoiceEndpoints
{
    public static IEndpointRouteBuilder MapOperationsInvoiceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        staff.MapGet("/invoices", async (HttpContext http, string? q, string? state, ListOperationsInvoicesUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            OperationsInvoiceFilter filter = Enum.TryParse(state, out OperationsInvoiceFilter chosen) && Enum.IsDefined(chosen) ? chosen : OperationsInvoiceFilter.All;
            string? text = q is { Length: <= 80 } ? q : null;
            ListOperationsInvoicesResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsInvoicesRequest { Text = text, Filter = filter },
                http.RequestAborted);
            return OperationsStaffResults.Page<InvoicesPage>(http, new
            {
                Company = await OperationsStaffResults.CompanyAsync(http, brand),
                Invoices = response.Invoices,
                Requests = response.Requests,
                Text = text,
                Filter = filter,
                Denied = !response.Succeeded,
                Error = errors.Describe(response.Error)
            });
        });

        staff.MapGet("/invoices/credits", async (HttpContext http, string? done, ListOperationsCreditRequestsUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors,
            IStringLocalizer<OperationsText> text) =>
            await RequestsAsync(http, null, Done(done, text), list, brand, errors));

        staff.MapGet("/invoices/{invoiceId:guid}", async (HttpContext http, Guid invoiceId, string? done, GetOperationsInvoiceUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
            await InvoiceAsync(http, invoiceId, null, Done(done, text), get, brand, errors));

        staff.MapGet("/invoices/{invoiceId:guid}/pdf", async (HttpContext http, Guid invoiceId, Guid? credit, GetOperationsInvoiceDocumentUseCase document) =>
        {
            GetOperationsInvoiceDocumentResponse response = await document.ExecuteAsync(OperationsCurrentUser.Context(http),
                new GetOperationsInvoiceDocumentRequest { InvoiceId = invoiceId, CreditNoteId = credit }, http.RequestAborted);
            return response.Succeeded ? OperationsStaffResults.File(http, response.Content, "application/pdf", response.FileName) : Results.NotFound();
        });

        // A credit note (an amount per line) or a void, with a reason; another person decides.
        staff.MapPost("/invoices/{invoiceId:guid}/credits", async (HttpContext http, Guid invoiceId, RequestOperationsCreditUseCase request, GetOperationsInvoiceUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            Dictionary<Guid, decimal> amounts = new();

            foreach (string field in form.Keys.Where(key => key.StartsWith("amount_", StringComparison.Ordinal)))
            {
                string typed = form[field].ToString();

                if (string.IsNullOrWhiteSpace(typed))
                {
                    continue;
                }

                if (!Guid.TryParseExact(field["amount_".Length..], "N", out Guid lineId) || OperationsCounterEndpoints.Amount(typed) is not { } amount)
                {
                    return await InvoiceAsync(http, invoiceId, OperationsBillingErrors.AmountInvalid, null, get, brand, errors);
                }

                amounts[lineId] = amount;
            }

            RequestOperationsCreditResponse response = await request.ExecuteAsync(OperationsCurrentUser.Context(http), new RequestOperationsCreditRequest
            {
                InvoiceId = invoiceId,
                Void = form["void"] == "true",
                Amounts = amounts,
                Reason = form["reason"],
                RequestKey = Guid.TryParseExact(form["key"], "N", out Guid key) ? key : Guid.Empty
            }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/invoices/" + invoiceId + "?done=requested")
                : await InvoiceAsync(http, invoiceId, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/invoices/credits/{creditNoteId:guid}/decide", async (HttpContext http, Guid creditNoteId, [FromForm] string? approve, [FromForm] string? note, [FromForm] string? back,
            DecideOperationsCreditUseCase decide, ListOperationsCreditRequestsUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            bool approved = approve == "true";
            DecideOperationsCreditResponse response = await decide.ExecuteAsync(OperationsCurrentUser.Context(http),
                new DecideOperationsCreditRequest { CreditNoteId = creditNoteId, Approve = approved, Note = note }, http.RequestAborted);
            string done = approved ? "approved" : "refused";

            if (!response.Succeeded)
            {
                return await RequestsAsync(http, response.Error, null, list, brand, errors);
            }

            return Results.Redirect(back == "invoice" ? "/staff/invoices/" + response.InvoiceId + "?done=" + done : "/staff/invoices/credits?done=" + done);
        });

        staff.MapGet("/settings/invoices", async (HttpContext http, string? done, GetOperationsInvoiceSettingsUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors,
            IStringLocalizer<OperationsText> text) =>
            await SettingsAsync(http, null, done is null ? null : text["ui.staff.done.saved"].Value, get, brand, errors));

        staff.MapPost("/settings/invoices", async (HttpContext http, [FromForm] string? taxName, [FromForm] string? taxNumber, [FromForm] string? freightRate,
            [FromForm] string? insuranceRate, [FromForm] string? suppliesRate, [FromForm] string? chargesRate, [FromForm] string? perKind, [FromForm] string? noteEn,
            [FromForm] string? noteFr, [FromForm] string? noteEs, SaveOperationsInvoiceSettingsUseCase save, GetOperationsInvoiceSettingsUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
        {
            SaveOperationsInvoiceSettingsResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), new SaveOperationsInvoiceSettingsRequest
            {
                TaxName = taxName,
                TaxNumber = taxNumber,
                FreightTaxPercent = Percent(freightRate),
                InsuranceTaxPercent = Percent(insuranceRate),
                SuppliesTaxPercent = Percent(suppliesRate),
                ChargesTaxPercent = Percent(chargesRate),
                TaxPerKind = perKind == "true",
                NoteEn = noteEn,
                NoteFr = noteFr,
                NoteEs = noteEs
            }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/settings/invoices?done=1")
                : OperationsStaffEndpoints.StepUp(response.Error, "/staff/settings/invoices") ?? await SettingsAsync(http, response.Error, null, get, brand, errors);
        });

        return endpoints;
    }

    // A rate as typed with a dot or a comma; anything else is refused by the rules (-1).
    private static decimal Percent(string? typed) =>
        string.IsNullOrWhiteSpace(typed) ? 0m : Vixly.Operations.Application.Charges.OperationsPriceText.Number(typed.Trim().Replace(',', '.')) ?? -1m;

    private static string? Done(string? done, IStringLocalizer<OperationsText> text) => done is "requested" or "approved" or "refused" ? text["ui.invoices.done." + done].Value : null;

    private static async Task<IResult> InvoiceAsync(HttpContext http, Guid invoiceId, Error? error, string? info, GetOperationsInvoiceUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsInvoiceResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsInvoiceRequest { InvoiceId = invoiceId }, http.RequestAborted);
        return OperationsStaffResults.Page<InvoicePage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Invoice = response.Succeeded ? response.Invoice : null,
            CreditNotes = response.CreditNotes,
            People = response.People,
            CanRequest = response.CanRequest,
            CanApprove = response.CanApprove,
            UserId = response.UserId,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }

    private static async Task<IResult> RequestsAsync(HttpContext http, Error? error, string? info, ListOperationsCreditRequestsUseCase list, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        ListOperationsCreditRequestsResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsCreditRequestsRequest(), http.RequestAborted);
        return OperationsStaffResults.Page<CreditRequestsPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Requests = response.Requests,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }

    private static async Task<IResult> SettingsAsync(HttpContext http, Error? error, string? info, GetOperationsInvoiceSettingsUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsInvoiceSettingsResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsInvoiceSettingsRequest(), http.RequestAborted);
        return OperationsStaffResults.Page<InvoiceSettingsPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Settings = response.Succeeded ? response.Settings : null,
            CanEdit = response.CanEdit,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }
}
