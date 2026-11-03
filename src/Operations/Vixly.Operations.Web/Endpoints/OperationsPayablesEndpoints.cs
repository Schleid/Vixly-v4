using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Payables;
using Vixly.Operations.Application.UseCases.Payables.ChangeOperationsBillSettings;
using Vixly.Operations.Application.UseCases.Payables.DecideOperationsBill;
using Vixly.Operations.Application.UseCases.Payables.GetOperationsBill;
using Vixly.Operations.Application.UseCases.Payables.GetOperationsProfit;
using Vixly.Operations.Application.UseCases.Payables.ListOperationsBills;
using Vixly.Operations.Application.UseCases.Payables.ListOperationsVendors;
using Vixly.Operations.Application.UseCases.Payables.PayOperationsBill;
using Vixly.Operations.Application.UseCases.Payables.SaveOperationsBill;
using Vixly.Operations.Application.UseCases.Payables.SaveOperationsVendor;
using Vixly.Operations.Application.UseCases.Payables.VoidOperationsBill;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Domain.Payables;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Payables;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsPayablesEndpoints.cs
//
// PURPOSE:
// Bills, vendors and the profit per shipment (Slice 8).
//
// RESPONSIBILITIES:
// - /staff/bills (list, new, one bill: correct, approve or reject, record a payment, void), /staff/vendors, /staff/profit and its CSV (/staff/profit/export: an address with an extension would skip the tenant)
//
// IMPORTANT NOTES:
// - Static forms with antiforgery and the form guard (ADR-0019); the use cases check every right; settings ask for the second check (bills are approved by a second person instead)
// =========================================================
public static class OperationsPayablesEndpoints
{
    public static IEndpointRouteBuilder MapOperationsPayablesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        staff.MapGet("/bills", async (HttpContext http, string? status, string? done, ListOperationsBillsUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors,
            IStringLocalizer<OperationsText> text) =>
            await ListAsync(http, Enum.TryParse(status, false, out OperationsBillStatus chosen) && Enum.IsDefined(chosen) ? chosen : null, null,
                done is "saved" ? text["ui.staff.done.saved"].Value : null, list, brand, errors));

        staff.MapPost("/bills/settings", async (HttpContext http, [FromForm] bool? needApproval, ChangeOperationsBillSettingsUseCase change, ListOperationsBillsUseCase list,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            ChangeOperationsBillSettingsResponse response = await change.ExecuteAsync(OperationsCurrentUser.Context(http),
                new ChangeOperationsBillSettingsRequest { NeedApproval = needApproval == true }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/bills?done=saved")
                : OperationsStaffEndpoints.StepUp(response.Error, "/staff/bills") ?? await ListAsync(http, null, response.Error, null, list, brand, errors);
        });

        staff.MapGet("/bills/new", async (HttpContext http, GetOperationsBillUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
            await BillAsync(http, null, null, null, get, brand, errors));

        staff.MapGet("/bills/{billId:guid}", async (HttpContext http, Guid billId, string? done, GetOperationsBillUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
            await BillAsync(http, billId, null, done is null ? null : text["ui.bills.done." + (done is "approved" or "rejected" or "paid" or "voided" ? done : "saved")].Value, get, brand,
                errors));

        staff.MapPost("/bills/new", async (HttpContext http, SaveOperationsBillUseCase save, GetOperationsBillUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) => await SaveAsync(http, null, save, get, brand, errors));

        staff.MapPost("/bills/{billId:guid}", async (HttpContext http, Guid billId, SaveOperationsBillUseCase save, GetOperationsBillUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) => await SaveAsync(http, billId, save, get, brand, errors));

        staff.MapPost("/bills/{billId:guid}/decide", async (HttpContext http, Guid billId, [FromForm] bool approve, [FromForm] string? reason, [FromForm] string? version, DecideOperationsBillUseCase decide,
            GetOperationsBillUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            DecideOperationsBillResponse response = await decide.ExecuteAsync(OperationsCurrentUser.Context(http),
                new DecideOperationsBillRequest { BillId = billId, Approve = approve, Reason = reason, Version = version }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/bills/" + billId + "?done=" + (approve ? "approved" : "rejected"))
                : OperationsStaffEndpoints.StepUp(response.Error, "/staff/bills/" + billId) ?? await BillAsync(http, billId, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/bills/{billId:guid}/pay", async (HttpContext http, Guid billId, [FromForm] string? paidOn, [FromForm] string? amount, [FromForm] string? method,
            [FromForm] string? reference, PayOperationsBillUseCase pay, GetOperationsBillUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            PayOperationsBillResponse response = await pay.ExecuteAsync(OperationsCurrentUser.Context(http), new PayOperationsBillRequest
            {
                BillId = billId,
                PaidOn = Day(paidOn) ?? DateOnly.MinValue,
                Amount = OperationsCounterEndpoints.Amount(amount) ?? -1m,
                Method = Enum.TryParse(method, false, out OperationsBillPaymentMethod chosen) ? chosen : 0,
                Reference = reference
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/bills/" + billId + "?done=paid")
                : OperationsStaffEndpoints.StepUp(response.Error, "/staff/bills/" + billId) ?? await BillAsync(http, billId, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/bills/{billId:guid}/void", async (HttpContext http, Guid billId, [FromForm] string? reason, VoidOperationsBillUseCase cancel, GetOperationsBillUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            VoidOperationsBillResponse response = await cancel.ExecuteAsync(OperationsCurrentUser.Context(http), new VoidOperationsBillRequest { BillId = billId, Reason = reason },
                http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/bills/" + billId + "?done=voided")
                : OperationsStaffEndpoints.StepUp(response.Error, "/staff/bills/" + billId) ?? await BillAsync(http, billId, response.Error, null, get, brand, errors);
        });

        staff.MapGet("/vendors", async (HttpContext http, string? done, ListOperationsVendorsUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors,
            IStringLocalizer<OperationsText> text) => await VendorsAsync(http, null, done is null ? null : text["ui.staff.done.saved"].Value, list, brand, errors));

        staff.MapPost("/vendors", async (HttpContext http, SaveOperationsVendorUseCase save, ListOperationsVendorsUseCase list, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) => await SaveVendorAsync(http, null, save, list, brand, errors));

        staff.MapPost("/vendors/{vendorId:guid}", async (HttpContext http, Guid vendorId, SaveOperationsVendorUseCase save, ListOperationsVendorsUseCase list,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) => await SaveVendorAsync(http, vendorId, save, list, brand, errors));

        staff.MapGet("/profit", async (HttpContext http, string? from, string? to, GetOperationsProfitUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            GetOperationsProfitResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsProfitRequest { From = Day(from), To = Day(to) },
                http.RequestAborted);
            return OperationsStaffResults.Page<ProfitPage>(http, new
            {
                Company = await OperationsStaffResults.CompanyAsync(http, brand),
                response.Rows,
                From = response.Succeeded ? response.From : Day(from) ?? DateOnly.FromDateTime(DateTime.UtcNow),
                To = response.Succeeded ? response.To : Day(to) ?? DateOnly.FromDateTime(DateTime.UtcNow),
                response.CurrencyCode,
                Error = errors.Describe(response.Error)
            });
        });

        staff.MapGet("/profit/export", async (HttpContext http, string? from, string? to, GetOperationsProfitUseCase get) =>
        {
            GetOperationsProfitResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsProfitRequest { From = Day(from), To = Day(to) },
                http.RequestAborted);

            if (!response.Succeeded)
            {
                return response.Error?.Code == OperationsPayableErrors.DatesInvalid.Code ? Results.BadRequest() : Results.Forbid();
            }

            StringBuilder csv = new("shipment,revenue,carrier_cost,other_cost,profit,margin_percent,estimated\r\n");

            foreach (OperationsShipmentProfit row in response.Rows)
            {
                csv.Append(Cell(row.Number)).Append(',').Append(Number(row.Revenue)).Append(',').Append(Number(row.CarrierCost)).Append(',').Append(Number(row.DirectCost)).Append(',')
                    .Append(Number(row.Profit)).Append(',').Append(row.Margin is { } margin ? Number(margin) : string.Empty).Append(',').Append(row.Estimated ? "yes" : "no").Append("\r\n");
            }

            return OperationsStaffResults.File(http, Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(), "text/csv",
                "profit-" + response.From.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "-" + response.To.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".csv");
        });

        return endpoints;
    }

    private static string Number(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    // A shipment number as a CSV cell: never read as a formula by a spreadsheet.
    private static string Cell(string value) =>
        "\"" + (value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r' ? "'" + value : value).Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    private static DateOnly? Day(string? typed) =>
        DateOnly.TryParseExact(typed, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly day) ? day : null;

    private static async Task<IResult> SaveAsync(HttpContext http, Guid? billId, SaveOperationsBillUseCase save, GetOperationsBillUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
        string[] party = form["party"].ToString().Split(':', 2);
        List<(string, decimal)> shares = new();

        for (int row = 0; row < BillPage.ShareRows; row++)
        {
            string number = form["share_number_" + row].ToString();
            string amount = form["share_amount_" + row].ToString();

            if (number.Length > 0 || amount.Length > 0)
            {
                shares.Add((number, OperationsCounterEndpoints.Amount(amount) ?? -1m));
            }
        }

        SaveOperationsBillResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), new SaveOperationsBillRequest
        {
            BillId = billId,
            Party = party.Length == 2 && Enum.TryParse(party[0], false, out OperationsBillParty kind) ? kind : 0,
            PartyId = party.Length == 2 && Guid.TryParse(party[1], out Guid partyId) ? partyId : Guid.Empty,
            Reference = form["reference"],
            Category = Enum.TryParse(form["category"].ToString(), false, out OperationsBillCategory category) ? category : 0,
            BilledOn = Day(form["billedOn"]) ?? DateOnly.MinValue,
            DueOn = Day(form["dueOn"]) ?? DateOnly.MinValue,
            Amount = OperationsCounterEndpoints.Amount(form["amount"]) ?? -1m,
            Note = form["note"],
            Shares = shares
        }, http.RequestAborted);
        return response.Succeeded ? Results.Redirect("/staff/bills/" + response.BillId + "?done=saved") : await BillAsync(http, billId, response.Error, null, get, brand, errors);
    }

    private static async Task<IResult> SaveVendorAsync(HttpContext http, Guid? vendorId, SaveOperationsVendorUseCase save, ListOperationsVendorsUseCase list,
        GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
        SaveOperationsVendorResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), new SaveOperationsVendorRequest
        {
            VendorId = vendorId,
            Name = form["name"],
            Category = Enum.TryParse(form["category"].ToString(), false, out OperationsBillCategory category) ? category : 0,
            Contact = form["contact"],
            Active = vendorId is null || form["active"].ToString() == "true"
        }, http.RequestAborted);
        return response.Succeeded ? Results.Redirect("/staff/vendors?done=saved") : await VendorsAsync(http, response.Error, null, list, brand, errors);
    }

    private static async Task<IResult> ListAsync(HttpContext http, OperationsBillStatus? status, Error? error, string? info, ListOperationsBillsUseCase list,
        GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        ListOperationsBillsResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsBillsRequest { Status = status }, http.RequestAborted);
        return OperationsStaffResults.Page<BillsPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.Bills,
            Status = status,
            response.Owed,
            response.Overdue,
            response.ToApprove,
            response.Today,
            response.CanEnter,
            response.NeedApproval,
            response.CanChangeSettings,
            response.CurrencyCode,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }

    private static async Task<IResult> BillAsync(HttpContext http, Guid? billId, Error? error, string? info, GetOperationsBillUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsBillResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsBillRequest { BillId = billId }, http.RequestAborted);

        if (billId is not null && response.Bill is null && response.Error?.Code == OperationsPayableErrors.NotFound.Code)
        {
            return Results.NotFound();
        }

        return OperationsStaffResults.Page<BillPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.Bill,
            response.Parties,
            response.CanEdit,
            response.CanDecide,
            response.CanPay,
            response.CanVoid,
            response.NeedApproval,
            response.CurrencyCode,
            response.Today,
            response.People,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }

    private static async Task<IResult> VendorsAsync(HttpContext http, Error? error, string? info, ListOperationsVendorsUseCase list, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        ListOperationsVendorsResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsVendorsRequest(), http.RequestAborted);
        return OperationsStaffResults.Page<VendorsPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.Vendors,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }
}
