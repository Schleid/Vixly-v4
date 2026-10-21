using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.UseCases.Credit.ListOperationsCreditAccounts;
using Vixly.Operations.Application.UseCases.Credit.GetOperationsCreditAccount;
using Vixly.Operations.Application.UseCases.Credit.OpenOperationsCreditAccount;
using Vixly.Operations.Application.UseCases.Credit.ChangeOperationsCreditAccount;
using Vixly.Operations.Application.UseCases.Credit.ListOperationsCreditCharges;
using Vixly.Operations.Application.UseCases.Credit.DecideOperationsCreditCharge;
using Vixly.Operations.Application.UseCases.Credit.GetOperationsCreditStatement;
using Vixly.Operations.Application.UseCases.Credit.GetOperationsCreditSettings;
using Vixly.Operations.Application.UseCases.Credit.SaveOperationsCreditSettings;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Credit;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsCreditEndpoints.cs
//
// PURPOSE:
// The credit department (Slice 5F step 2b): accounts, an account and its statement, charges to approve, Settings → Credit.
//
// RESPONSIBILITIES:
// - /staff/credit and its forms; /staff/settings/credit
//
// IMPORTANT NOTES:
// - Static forms with antiforgery and the form guard (ADR-0019); opening and changing an account ask for the second check
// - The use cases check every permission
// =========================================================
public static class OperationsCreditEndpoints
{
    public static IEndpointRouteBuilder MapOperationsCreditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        staff.MapGet("/credit", async (HttpContext http, ListOperationsCreditAccountsUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
            await AccountsAsync(http, null, list, brand, errors));

        staff.MapPost("/credit/accounts", async (HttpContext http, [FromForm] Guid? customer, [FromForm] string? code, [FromForm] string? limit, [FromForm] string? terms,
            OpenOperationsCreditAccountUseCase open, ListOperationsCreditAccountsUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            OpenOperationsCreditAccountResponse response = await open.ExecuteAsync(OperationsCurrentUser.Context(http), new OpenOperationsCreditAccountRequest
            {
                CustomerId = customer,
                CustomerCode = code,
                Limit = OperationsCounterEndpoints.Amount(limit) ?? -1m,
                TermsDays = Days(terms)
            }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/credit/customers/" + response.CustomerId + "?done=opened")
                : OperationsStaffEndpoints.StepUp(response.Error, "/staff/credit") ?? await AccountsAsync(http, response.Error, list, brand, errors);
        });

        staff.MapGet("/credit/customers/{customerId:guid}", async (HttpContext http, Guid customerId, string? done, GetOperationsCreditAccountUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> texts) =>
            await AccountAsync(http, customerId, null, done is "opened" or "changed" ? texts["ui.credit.done." + done].Value : null, get, brand, errors));

        staff.MapPost("/credit/customers/{customerId:guid}", async (HttpContext http, Guid customerId, [FromForm] string? limit, [FromForm] string? terms, [FromForm] string? status,
            [FromForm] string? reason, ChangeOperationsCreditAccountUseCase change, GetOperationsCreditAccountUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            ChangeOperationsCreditAccountResponse response = await change.ExecuteAsync(OperationsCurrentUser.Context(http), new ChangeOperationsCreditAccountRequest
            {
                CustomerId = customerId,
                Limit = OperationsCounterEndpoints.Amount(limit) ?? -1m,
                TermsDays = Days(terms),
                Status = Enum.TryParse(status, false, out OperationsCreditAccountStatus parsed) ? parsed : 0,
                Reason = reason
            }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/credit/customers/" + customerId + "?done=changed")
                : OperationsStaffEndpoints.StepUp(response.Error, "/staff/credit/customers/" + customerId) ?? await AccountAsync(http, customerId, response.Error, null, get, brand, errors);
        });

        staff.MapGet("/credit/customers/{customerId:guid}/statement", async (HttpContext http, Guid customerId, GetOperationsCreditStatementUseCase statement) =>
        {
            GetOperationsCreditStatementResponse response = await statement.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsCreditStatementRequest { CustomerId = customerId },
                http.RequestAborted);
            return response.Succeeded ? OperationsStaffResults.File(http, response.Pdf, "application/pdf", null) : Results.NotFound();
        });

        staff.MapGet("/credit/charges", async (HttpContext http, string? done, ListOperationsCreditChargesUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors,
            IStringLocalizer<OperationsText> texts) =>
            await ChargesAsync(http, null, done is null ? null : texts["ui.credit.done.decided"].Value, list, brand, errors));

        staff.MapPost("/credit/charges/{invoiceId:guid}", async (HttpContext http, Guid invoiceId, [FromForm] bool approve, [FromForm] string? note, DecideOperationsCreditChargeUseCase decide,
            ListOperationsCreditChargesUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            DecideOperationsCreditChargeResponse response = await decide.ExecuteAsync(OperationsCurrentUser.Context(http),
                new DecideOperationsCreditChargeRequest { InvoiceId = invoiceId, Approve = approve, Note = note }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/credit/charges?done=1")
                : OperationsStaffEndpoints.StepUp(response.Error, "/staff/credit/charges") ?? await ChargesAsync(http, response.Error, null, list, brand, errors);
        });

        staff.MapGet("/settings/credit", async (HttpContext http, string? done, GetOperationsCreditSettingsUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors,
            IStringLocalizer<OperationsText> texts) =>
            await SettingsAsync(http, null, done is null ? null : texts["ui.staff.done.saved"].Value, get, brand, errors));

        staff.MapPost("/settings/credit", async (HttpContext http, [FromForm] string? terms, [FromForm] string? overLimit, [FromForm] bool? statements, SaveOperationsCreditSettingsUseCase save,
            GetOperationsCreditSettingsUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            SaveOperationsCreditSettingsResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), new SaveOperationsCreditSettingsRequest
            {
                TermsDays = Days(terms) ?? -1,
                OverLimit = Enum.TryParse(overLimit, false, out OperationsCreditOverLimit rule) ? rule : 0,
                Statements = statements == true
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/settings/credit?done=1") : await SettingsAsync(http, response.Error, null, get, brand, errors);
        });

        return endpoints;
    }

    // Days typed: empty is none (the company's terms); anything unreadable is refused by the domain.
    private static int? Days(string? typed) =>
        string.IsNullOrWhiteSpace(typed) ? null : int.TryParse(typed.Trim(), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int days) ? days : -1;

    private static async Task<IResult> AccountsAsync(HttpContext http, Error? error, ListOperationsCreditAccountsUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        ListOperationsCreditAccountsResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsCreditAccountsRequest(), http.RequestAborted);

        if (!response.Succeeded && error is null)
        {
            return Results.Forbid();
        }

        return OperationsStaffResults.Page<CreditAccountsPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.Rows,
            response.Awaiting,
            response.CreditOn,
            response.CurrencyCode,
            response.Marked,
            Error = errors.Describe(error ?? response.Error)
        });
    }

    private static async Task<IResult> AccountAsync(HttpContext http, Guid customerId, Error? error, string? info, GetOperationsCreditAccountUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsCreditAccountResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsCreditAccountRequest { CustomerId = customerId },
            http.RequestAborted);

        if (!response.Succeeded)
        {
            return Results.NotFound();
        }

        return OperationsStaffResults.Page<CreditAccountPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.Customer,
            response.Account,
            response.Items,
            response.Position,
            response.CompanyTermsDays,
            response.CurrencyCode,
            response.Today,
            Error = errors.Describe(error),
            Info = info
        });
    }

    private static async Task<IResult> ChargesAsync(HttpContext http, Error? error, string? info, ListOperationsCreditChargesUseCase list, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        ListOperationsCreditChargesResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsCreditChargesRequest(), http.RequestAborted);

        if (!response.Succeeded)
        {
            return Results.Forbid();
        }

        return OperationsStaffResults.Page<CreditChargesPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.Invoices,
            response.Accounts,
            Error = errors.Describe(error),
            Info = info
        });
    }

    private static async Task<IResult> SettingsAsync(HttpContext http, Error? error, string? info, GetOperationsCreditSettingsUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsCreditSettingsResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsCreditSettingsRequest(), http.RequestAborted);

        if (!response.Succeeded)
        {
            return Results.Forbid();
        }

        return OperationsStaffResults.Page<CreditSettingsPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.Settings,
            response.CanEdit,
            Error = errors.Describe(error),
            Info = info
        });
    }
}
