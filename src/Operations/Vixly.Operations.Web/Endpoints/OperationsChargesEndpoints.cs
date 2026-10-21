using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.UseCases.Charges.GetOperationsChargesSetup;
using Vixly.Operations.Application.UseCases.Charges.RemoveOperationsScheduledVersion;
using Vixly.Operations.Application.UseCases.Charges.SaveOperationsChargeExtra;
using Vixly.Operations.Application.UseCases.Charges.SaveOperationsInsurancePlan;
using Vixly.Operations.Application.UseCases.Charges.SaveOperationsItemType;
using Vixly.Operations.Application.UseCases.Charges.SaveOperationsPriceList;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Charges;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsChargesEndpoints.cs
//
// PURPOSE:
// The company's prices, one transport mode at a time (Slice 4, 4B and 4C).
//
// RESPONSIBILITIES:
// - /staff/charges: price lists with dated versions, item types, extras and the insurance plan
// - Remove a version scheduled for later
//
// IMPORTANT NOTES:
// - Static forms with antiforgery and the form guard (ADR-0019)
// - The use cases check every permission; the menu only hides what is not allowed
// =========================================================
public static class OperationsChargesEndpoints
{
    public static IEndpointRouteBuilder MapOperationsChargesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        // Slice 4C: one mode at a time (?mode=air|ocean|ground).
        staff.MapGet("/charges", async (HttpContext http, string? done, string? mode, GetOperationsChargesSetupUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors,
            IStringLocalizer<OperationsText> text) =>
            await ChargesAsync(http, Mode(mode), null, done == "saved" ? text["ui.charges.done.saved"].Value : null, get, brand, errors));

        // [FromForm] turns on the antiforgery check; the band rows and item prices are read from the same form.
        staff.MapPost("/charges/cards", async (HttpContext http, [FromForm] Guid? card, [FromForm] string? service, [FromForm] string? origin, [FromForm] string? destination,
            [FromForm] string? customer, [FromForm] string? customs, [FromForm] string? mode, [FromForm] string? active, SaveOperationsPriceListUseCase save,
            GetOperationsChargesSetupUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            OperationsTransportMode tab = Mode(mode);
            OperationsPriceForm prices = OperationsPriceForm.Read(http.Request.Form);

            if (!prices.EffectiveRead)
            {
                return await ChargesAsync(http, tab, OperationsChargeErrors.EffectiveInvalid, null, get, brand, errors);
            }

            if (!prices.Valid)
            {
                return await ChargesAsync(http, tab, OperationsChargeErrors.BandsInvalid, null, get, brand, errors);
            }

            SaveOperationsPriceListResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), new SaveOperationsPriceListRequest
            {
                CardId = card,
                ServiceCode = service,
                OriginWarehouseId = Id(origin),
                DestinationWarehouseId = Id(destination),
                Customs = Choice(customs, out OperationsCustomsPolicy policy) ? policy : OperationsCustomsPolicy.TransportOnly,
                CustomerCode = customer,
                Bands = prices.Bands,
                Items = prices.Items,
                MinimumCharge = prices.MinimumCharge,
                ServiceFee = prices.ServiceFee,
                ConsolidationFee = prices.ConsolidationFee,
                PerPackage = prices.PerPackage,
                PerShipment = prices.PerShipment,
                EffectiveFrom = prices.EffectiveFrom,
                Active = active == "true"
            }, http.RequestAborted);
            return response.Succeeded ? Saved(tab) : await ChargesAsync(http, tab, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/charges/versions/remove", async (HttpContext http, [FromForm] string? owner, [FromForm(Name = "owner_id")] Guid? ownerId, [FromForm] Guid? version,
            [FromForm(Name = "tab")] string? mode, RemoveOperationsScheduledVersionUseCase remove, GetOperationsChargesSetupUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            OperationsTransportMode tab = Mode(mode);

            if (!Choice(owner, out OperationsVersionOwner chosen) || ownerId is null || version is null)
            {
                return await ChargesAsync(http, tab, OperationsChargeErrors.VersionNotFound, null, get, brand, errors);
            }

            RemoveOperationsScheduledVersionResponse response = await remove.ExecuteAsync(OperationsCurrentUser.Context(http),
                new RemoveOperationsScheduledVersionRequest { Owner = chosen, OwnerId = ownerId.Value, VersionId = version.Value }, http.RequestAborted);
            return response.Succeeded ? Saved(tab) : await ChargesAsync(http, tab, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/charges/item-types", async (HttpContext http, [FromForm] Guid? type, [FromForm] string? name, [FromForm] string? words, [FromForm] string? active,
            SaveOperationsItemTypeUseCase save, GetOperationsChargesSetupUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            SaveOperationsItemTypeResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http),
                new SaveOperationsItemTypeRequest { ItemTypeId = type, Name = name, Words = words, Active = active == "true" }, http.RequestAborted);
            return response.Succeeded ? Saved(OperationsTransportMode.Ocean) : await ChargesAsync(http, OperationsTransportMode.Ocean, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/charges/extras", async (HttpContext http, [FromForm] Guid? extra, [FromForm] string? name, [FromForm] string? government, [FromForm] string? amount,
            [FromForm] string? calculation, [FromForm] string? per, [FromForm] string? trigger, [FromForm] string? words, [FromForm] string? service, [FromForm] string? country,
            [FromForm] string? mode, [FromForm] string? origin, [FromForm] string? destination, [FromForm] string? delivery, [FromForm(Name = "tab")] string? shown,
            [FromForm] string? active, SaveOperationsChargeExtraUseCase save, GetOperationsChargesSetupUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            OperationsTransportMode tab = Mode(shown);
            (DateOnly? effective, bool dayRead) = OperationsPriceForm.Day(http.Request.Form);

            if (!dayRead)
            {
                return await ChargesAsync(http, tab, OperationsChargeErrors.EffectiveInvalid, null, get, brand, errors);
            }

            if (!Choice(calculation, out OperationsExtraCalculation chosenCalculation) || !Choice(per, out OperationsExtraPer chosenPer) || !Choice(trigger, out OperationsExtraTrigger chosenTrigger)
                || !Choice(delivery ?? "None", out OperationsDeliveryModes chosenDelivery) || (!string.IsNullOrEmpty(mode) && !Choice(mode, out OperationsTransportMode _)))
            {
                return await ChargesAsync(http, tab, OperationsChargeErrors.ChoiceInvalid, null, get, brand, errors);
            }

            SaveOperationsChargeExtraResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), new SaveOperationsChargeExtraRequest
            {
                ExtraId = extra,
                Name = name,
                IsGovernmentFee = government == "true",
                Amount = OperationsPriceText.Number(amount),
                Calculation = chosenCalculation,
                Per = chosenPer,
                Trigger = chosenTrigger,
                Words = words,
                ServiceCode = service,
                CountryCode = country,
                Mode = Choice(mode, out OperationsTransportMode extraMode) ? extraMode : null,
                OriginWarehouseId = Id(origin),
                DestinationWarehouseId = Id(destination),
                Delivery = chosenDelivery,
                EffectiveFrom = effective,
                Active = active == "true"
            }, http.RequestAborted);
            return response.Succeeded ? Saved(tab) : await ChargesAsync(http, tab, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/charges/insurance", async (HttpContext http, [FromForm] string? method, [FromForm] string? percent, [FromForm] string? minimum, [FromForm] string? bands,
            [FromForm(Name = "max_value")] string? maxValue, [FromForm] string? mode, [FromForm] string? active, [FromForm] Guid? plan, [FromForm] string? name,
            [FromForm] string? position, [FromForm(Name = "pitch_en")] string? pitchEn, [FromForm(Name = "pitch_fr")] string? pitchFr, [FromForm(Name = "pitch_es")] string? pitchEs,
            SaveOperationsInsurancePlanUseCase save,
            GetOperationsChargesSetupUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            OperationsTransportMode tab = Mode(mode);
            (DateOnly? effective, bool dayRead) = OperationsPriceForm.Day(http.Request.Form);

            if (!dayRead)
            {
                return await ChargesAsync(http, tab, OperationsChargeErrors.EffectiveInvalid, null, get, brand, errors);
            }

            OperationsInsuranceCoverageTerms? coverage = OperationsInsuranceForm.Read(http.Request.Form);

            if (!Choice(method, out OperationsInsuranceMethod chosen) || !Amounts(out decimal?[] amounts, percent, minimum, maxValue) || coverage is null)
            {
                return await ChargesAsync(http, tab, OperationsChargeErrors.InsuranceInvalid, null, get, brand, errors);
            }

            SaveOperationsInsurancePlanResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), new SaveOperationsInsurancePlanRequest
            {
                Mode = tab,
                PlanId = plan,
                Name = name,
                PitchEn = pitchEn,
                PitchFr = pitchFr,
                PitchEs = pitchEs,
                Position = int.TryParse(position, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int place) ? place : 0,
                Method = chosen,
                Percent = amounts[0],
                MinimumPremium = amounts[1],
                MaxValue = amounts[2],
                BandsText = bands,
                Coverage = coverage,
                EffectiveFrom = effective,
                Active = active == "true"
            }, http.RequestAborted);
            return response.Succeeded ? Saved(tab) : await ChargesAsync(http, tab, response.Error, null, get, brand, errors);
        });

        return endpoints;
    }

    // Each value empty (null) or a plain number; false when one cannot be read.
    internal static bool Amounts(out decimal?[] amounts, params string?[] values)
    {
        amounts = values.Select(value => string.IsNullOrWhiteSpace(value) ? null : OperationsPriceText.Number(value)).ToArray();
        return values.Zip(amounts).All(pair => string.IsNullOrWhiteSpace(pair.First) || pair.Second is not null);
    }

    // A select's empty choice ("any origin", "no hub") comes as an empty text, which a Guid? parameter would refuse with 400; anything else unreadable is an unknown id.
    internal static Guid? Id(string? value) => string.IsNullOrEmpty(value) ? null : Guid.TryParse(value, out Guid id) ? id : Guid.Empty;

    internal static bool Choice<T>(string? value, out T chosen)
        where T : struct, Enum =>
        Enum.TryParse(value, ignoreCase: false, out chosen) && Enum.IsDefined(chosen) && !value!.Any(char.IsDigit);

    // Slice 4C: the tab to show; Air when missing or unknown.
    internal static OperationsTransportMode Mode(string? value) =>
        Enum.TryParse(value, ignoreCase: true, out OperationsTransportMode mode) && Enum.IsDefined(mode) && !(value ?? string.Empty).Any(char.IsDigit) ? mode : OperationsTransportMode.Air;

    private static IResult Saved(OperationsTransportMode mode) => Results.Redirect("/staff/charges?mode=" + mode.ToString().ToLowerInvariant() + "&done=saved");

    private static async Task<IResult> ChargesAsync(HttpContext http, OperationsTransportMode mode, Error? error, string? info, GetOperationsChargesSetupUseCase get,
        GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        GetOperationsChargesSetupResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsChargesSetupRequest(), http.RequestAborted);
        return OperationsStaffResults.Page<ChargesPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Mode = mode,
            Cards = response.Cards,
            Extras = response.Extras,
            Insurance = response.Insurance,
            Services = response.Services,
            ItemTypes = response.ItemTypes,
            Origins = response.Origins,
            Destinations = response.Destinations,
            Currency = response.CurrencyCode,
            WeightUnit = response.WeightUnit,
            DimensionUnit = response.DimensionUnit,
            UtcNow = response.UtcNow,
            Denied = !response.Succeeded,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }
}
