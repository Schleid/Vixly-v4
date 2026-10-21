using System.Globalization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Application.UseCases.Audit.ListOperationsAuditEntries;
using Vixly.Operations.Application.UseCases.Settings.GetOperationsTenantSettings;
using Vixly.Operations.Application.UseCases.Settings.ListOperationsShippingServices;
using Vixly.Operations.Application.UseCases.Settings.SaveOperationsShippingServices;
using Vixly.Operations.Application.UseCases.Settings.UpdateOperationsTenantSettings;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Application.UseCases.Staff.GetOperationsStaffMember;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Settings;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsSettingsEndpoints.cs
//
// PURPOSE:
// The settings page (Slice 1E).
//
// RESPONSIBILITIES:
// - /staff/settings: show and save the tenant settings
//
// IMPORTANT NOTES:
// - Static form with antiforgery (ADR-0019); the use case checks the permission and step-up
// =========================================================
public static class OperationsSettingsEndpoints
{
    public static IEndpointRouteBuilder MapOperationsSettingsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // Every change under /staff checks the antiforgery token, also when the form carries no other field (ADR-0019).
        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        staff.MapGet("/settings", async (HttpContext http, string? done, GetOperationsTenantSettingsUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText text, IStringLocalizer<OperationsText> texts) =>
            await PageAsync(http, get, brand, text, null, done is null ? null : texts["ui.staff.done.saved"].Value));

        staff.MapPost("/settings", async (HttpContext http, [FromForm] string? currencyCode, [FromForm] string? weightUnit, [FromForm] string? dimensionUnit,
            [FromForm] bool? allowCollect, [FromForm] bool? allowCredit, [FromForm] bool? pickup, [FromForm] bool? homeDelivery, [FromForm] bool? requireSecondFactor,
            [FromForm] string? shipmentPrefix, [FromForm] string? receivingPrefix, [FromForm] string? invoicePrefix, [FromForm] string? paymentPrefix, [FromForm] string? customerPrefix, [FromForm] string? parcelPrefix, [FromForm] string? supplyOrderPrefix,
            [FromForm] string? creditNotePrefix, [FromForm] string? bundlePrefix, [FromForm] string? boxPrefix, [FromForm] string? dispatchPrefix,
            [FromForm] string? timeZone, UpdateOperationsTenantSettingsUseCase update, GetOperationsTenantSettingsUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            UpdateOperationsTenantSettingsResponse response = await update.ExecuteAsync(OperationsCurrentUser.Context(http), new UpdateOperationsTenantSettingsRequest
            {
                CurrencyCode = currencyCode ?? string.Empty,
                WeightUnit = Enum.TryParse(weightUnit, false, out OperationsWeightUnit weight) ? weight : 0,
                DimensionUnit = Enum.TryParse(dimensionUnit, false, out OperationsDimensionUnit dimension) ? dimension : 0,
                AllowCollect = allowCollect == true,
                AllowCredit = allowCredit == true,
                DeliveryModes = (pickup == true ? OperationsDeliveryModes.Pickup : 0) | (homeDelivery == true ? OperationsDeliveryModes.HomeDelivery : 0),
                RequireSecondFactorForAllStaff = requireSecondFactor == true,
                ShipmentPrefix = shipmentPrefix ?? string.Empty,
                ReceivingPrefix = receivingPrefix ?? string.Empty,
                InvoicePrefix = invoicePrefix ?? string.Empty,
                PaymentPrefix = paymentPrefix ?? string.Empty,
                CustomerPrefix = customerPrefix,
                ParcelPrefix = parcelPrefix,
                SupplyOrderPrefix = supplyOrderPrefix,
                CreditNotePrefix = creditNotePrefix,
                BundlePrefix = bundlePrefix,
                BoxPrefix = boxPrefix,
                DispatchPrefix = dispatchPrefix,
                TimeZone = timeZone ?? string.Empty
            }, http.RequestAborted);

            return response.Succeeded
                ? Results.Redirect("/staff/settings?done=1")
                : OperationsStaffEndpoints.StepUp(response.Error, "/staff/settings") ?? await PageAsync(http, get, brand, text, response.Error, null);
        });

        staff.MapGet("/services", async (HttpContext http, string? done, ListOperationsShippingServicesUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText text, IStringLocalizer<OperationsText> texts) =>
            await ServicesPageAsync(http, list, brand, text, null, done is null ? null : texts["ui.staff.done.saved"].Value));

        staff.MapPost("/services", async (HttpContext http, SaveOperationsShippingServicesUseCase save, ListOperationsShippingServicesUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            HashSet<string> offered = form["offered"].Where(code => code is not null).Select(code => code!).ToHashSet(StringComparer.Ordinal);
            List<OperationsShippingServiceRow> rows = Vixly.Contracts.Platform.Catalog.PlatformShippingServices.All.Select(service => new OperationsShippingServiceRow(
                service.Code,
                service.Mode,
                offered.Contains(service.Code),
                form["label_" + service.Code].ToString(),
                int.TryParse(form["min_" + service.Code], NumberStyles.Integer, CultureInfo.InvariantCulture, out int min) ? min : 0,
                int.TryParse(form["max_" + service.Code], NumberStyles.Integer, CultureInfo.InvariantCulture, out int max) ? max : 0,
                decimal.TryParse(form["divisor_" + service.Code], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal divisor) ? divisor : null,
                service.DefaultVolumetricDivisor is not null)).ToList();

            SaveOperationsShippingServicesResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), new SaveOperationsShippingServicesRequest { Services = rows }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/services?done=1") : await ServicesPageAsync(http, list, brand, text, response.Error, null);
        });

        staff.MapGet("/audit", async (HttpContext http, Guid? actor, string? area, string? from, string? to, int? page,
            ListOperationsAuditEntriesUseCase list, GetOperationsStaffMemberUseCase member, GetOperationsSignInBrandUseCase brand, OperationsErrorText text) =>
        {
            string? chosenArea = area is not null && AuditPage.Areas.Contains(area, StringComparer.Ordinal) ? area : null;
            DateOnly? fromDate = DateOnly.TryParseExact(from, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly f) ? f : null;
            DateOnly? toDate = DateOnly.TryParseExact(to, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly t) ? t : null;
            int current = Math.Clamp(page ?? 1, 1, 100_000);

            ListOperationsAuditEntriesResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsAuditEntriesRequest
            {
                ActorUserId = actor,
                Area = chosenArea,
                FromUtc = fromDate is { } start ? new DateTimeOffset(start.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null,
                ToUtc = toDate is { } end ? new DateTimeOffset(end.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null,
                Page = current
            }, http.RequestAborted);

            string? actorName = actor is { } id && response.Succeeded
                ? (await member.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsStaffMemberRequest { UserId = id }, http.RequestAborted)).Member?.FullName
                : null;
            GetOperationsSignInBrandResponse company = await brand.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsSignInBrandRequest(), http.RequestAborted);
            http.Response.Headers.CacheControl = "no-store";

            return new RazorComponentResult<AuditPage>(new
            {
                Company = company.DisplayName,
                Lines = response.Lines,
                TotalCount = response.TotalCount,
                Page = current,
                PageSize = ListOperationsAuditEntriesUseCase.PageSize,
                ActorUserId = actor,
                ActorName = actorName,
                Area = chosenArea,
                From = fromDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                To = toDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Error = text.Describe(response.Error)
            });
        });

        return endpoints;
    }

    private static async Task<IResult> ServicesPageAsync(HttpContext http, ListOperationsShippingServicesUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText text, Error? error, string? info)
    {
        ListOperationsShippingServicesResponse services = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsShippingServicesRequest(), http.RequestAborted);
        GetOperationsSignInBrandResponse company = await brand.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsSignInBrandRequest(), http.RequestAborted);
        http.Response.Headers.CacheControl = "no-store";

        return new RazorComponentResult<ShippingServicesPage>(new
        {
            Company = company.DisplayName,
            Services = services.Services,
            DivisorUnit = services.DivisorUnit.Length == 0 ? "cm3/kg" : services.DivisorUnit,
            CanEdit = services.CanEdit,
            Error = text.Describe(error ?? services.Error),
            Info = info
        });
    }

    private static async Task<IResult> PageAsync(HttpContext http, GetOperationsTenantSettingsUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText text, Error? error, string? info)
    {
        GetOperationsTenantSettingsResponse settings = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsTenantSettingsRequest(), http.RequestAborted);
        GetOperationsSignInBrandResponse company = await brand.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsSignInBrandRequest(), http.RequestAborted);
        http.Response.Headers.CacheControl = "no-store";

        if (settings.View is null)
        {
            return new RazorComponentResult<Vixly.Operations.Web.Components.Editor.EditorMessagePage>(new
            {
                Title = http.RequestServices.GetRequiredService<IStringLocalizer<OperationsText>>()["ui.settings.title"].Value,
                Company = company.DisplayName,
                Section = "settings",
                Error = text.Describe(settings.Error)
            });
        }

        return new RazorComponentResult<SettingsPage>(new
        {
            Company = company.DisplayName,
            Settings = settings.View.Settings,
            CurrencyLocked = settings.View.CurrencyLocked,
            CanEdit = settings.View.CanEdit,
            Error = text.Describe(error),
            Info = info
        });
    }
}
