using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.UseCases.Inspection.ChangeOperationsInspectionShare;
using Vixly.Operations.Application.UseCases.Inspection.CopyOperationsStartingList;
using Vixly.Operations.Application.UseCases.Inspection.DecideOperationsDisposal;
using Vixly.Operations.Application.UseCases.Inspection.FinishOperationsInspection;
using Vixly.Operations.Application.UseCases.Inspection.GetOperationsInspection;
using Vixly.Operations.Application.UseCases.Inspection.GetOperationsInspectionSetup;
using Vixly.Operations.Application.UseCases.Inspection.ListOperationsHolds;
using Vixly.Operations.Application.UseCases.Inspection.ListOperationsInspectionQueue;
using Vixly.Operations.Application.UseCases.Inspection.ReleaseOperationsHold;
using Vixly.Operations.Application.UseCases.Inspection.SaveOperationsInspectionRule;
using Vixly.Operations.Application.UseCases.Inspection.SaveOperationsRestrictedItem;
using Vixly.Operations.Application.UseCases.Inspection.StartOperationsInspection;
using Vixly.Operations.Application.UseCases.Receiving.FindOperationsParcelByScan;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Domain.Inspection;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Tracking;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Inspection;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsInspectionWorkEndpoints.cs
//
// PURPOSE:
// Compliance inspection after the station: the company's list and rules, the queue, the inspection and the holds (Slice 3C).
//
// RESPONSIBILITIES:
// - /staff/inspection/setup: the restricted-items list, rules and random share
// - /staff/inspection/queue and /staff/inspection/queue/{id}: the queue and one inspection
// - /staff/holds: open holds, releases and disposal decisions
//
// IMPORTANT NOTES:
// - Static forms with antiforgery and the form guard; each command carries its own reference (ADR-0019)
// - The use cases check every permission; the menu only hides what is not allowed
// =========================================================
public static class OperationsInspectionWorkEndpoints
{
    // Six phone photos of up to 10 MB each, and the form fields.
    private const long UploadLimit = (OperationsReceivingLimits.MaxPhotosPerReceipt * (long)OperationsReceivingLimits.MaxPhotoBytes) + (1024 * 1024);

    public static IEndpointRouteBuilder MapOperationsInspectionWorkEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        staff.MapGet("/inspection/setup", async (HttpContext http, string? done, GetOperationsInspectionSetupUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors,
            IStringLocalizer<OperationsText> text) =>
            await SetupAsync(http, null, done is "saved" or "copied" ? text["ui.inspection.setup.done." + done].Value : null, get, brand, errors));

        staff.MapPost("/inspection/setup/share", async (HttpContext http, [FromForm] string? percent, ChangeOperationsInspectionShareUseCase change, GetOperationsInspectionSetupUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            ChangeOperationsInspectionShareResponse response = await change.ExecuteAsync(OperationsCurrentUser.Context(http),
                new ChangeOperationsInspectionShareRequest { Percent = int.TryParse(percent, NumberStyles.None, CultureInfo.InvariantCulture, out int value) ? value : -1 }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/inspection/setup?done=saved") : await SetupAsync(http, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/inspection/setup/rules", async (HttpContext http, [FromForm] Guid? rule, [FromForm] string? kind, [FromForm] string? value, [FromForm] string? active,
            SaveOperationsInspectionRuleUseCase save, GetOperationsInspectionSetupUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            if (rule is null && !Choice(kind, out OperationsInspectionRuleKind _))
            {
                return await SetupAsync(http, OperationsInspectionErrors.RuleKindInvalid, null, get, brand, errors);
            }

            Choice(kind, out OperationsInspectionRuleKind chosen);
            SaveOperationsInspectionRuleResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http),
                new SaveOperationsInspectionRuleRequest { RuleId = rule, Kind = chosen, Value = value, Active = active == "true" }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/inspection/setup?done=saved") : await SetupAsync(http, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/inspection/setup/items", async (HttpContext http, [FromForm] Guid? item, [FromForm] string? name, [FromForm] string? words, [FromForm] string? kind,
            [FromForm] string? countries, [FromForm] string? carriers, [FromForm] string? requirement, [FromForm] string? active, SaveOperationsRestrictedItemUseCase save,
            GetOperationsInspectionSetupUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            if (!Choice(kind, out OperationsRestrictedItemKind chosen))
            {
                return await SetupAsync(http, OperationsInspectionErrors.RuleKindInvalid, null, get, brand, errors);
            }

            SaveOperationsRestrictedItemResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), new SaveOperationsRestrictedItemRequest
            {
                ItemId = item,
                Name = name,
                Words = words,
                Kind = chosen,
                Countries = countries,
                Carriers = carriers,
                Requirement = requirement,
                Active = active == "true"
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/inspection/setup?done=saved") : await SetupAsync(http, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/inspection/setup/starting-list", async (HttpContext http, CopyOperationsStartingListUseCase copy, GetOperationsInspectionSetupUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            CopyOperationsStartingListResponse response = await copy.ExecuteAsync(OperationsCurrentUser.Context(http),
                new CopyOperationsStartingListRequest { Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName }, http.RequestAborted);

            // A second click finds the list already copied: that is the result the person wanted.
            return response.Succeeded || response.Error!.Equals(OperationsInspectionErrors.ListNotEmpty)
                ? Results.Redirect("/staff/inspection/setup?done=copied")
                : await SetupAsync(http, response.Error, null, get, brand, errors);
        });

        staff.MapGet("/inspection/queue", async (HttpContext http, Guid? warehouse, int? page, string? code, string? done, ListOperationsInspectionQueueUseCase list,
            FindOperationsParcelByScanUseCase find, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
        {
            Error? error = null;

            // Scanning an ID label opens the parcel's inspection when it is in the queue.
            if (!string.IsNullOrWhiteSpace(code))
            {
                FindOperationsParcelByScanResponse found = await find.ExecuteAsync(OperationsCurrentUser.Context(http), new FindOperationsParcelByScanRequest { Barcode = code }, http.RequestAborted);
                error = found.Error;

                if (found.Succeeded)
                {
                    ListOperationsInspectionQueueResponse all = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsInspectionQueueRequest { Page = 1, ParcelId = found.ParcelId }, http.RequestAborted);
                    error = all.Error ?? OperationsInspectionErrors.NotFound;

                    if (all.Items.FirstOrDefault(item => item.Parcel.ParcelId == found.ParcelId) is { } match)
                    {
                        return Results.Redirect("/staff/inspection/queue/" + match.Inspection.Id);
                    }
                }
            }

            ListOperationsInspectionQueueResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http),
                new ListOperationsInspectionQueueRequest { WarehouseId = warehouse, Page = Math.Max(page ?? 1, 1) }, http.RequestAborted);
            return OperationsStaffResults.Page<InspectionQueuePage>(http, new
            {
                Company = await OperationsStaffResults.CompanyAsync(http, brand),
                Items = response.Items,
                TotalCount = response.TotalCount,
                Page = Math.Max(page ?? 1, 1),
                WarehouseId = warehouse,
                Warehouses = response.Warehouses,
                Error = errors.Describe(error ?? response.Error),
                Info = done is "finished" ? text["ui.inspection.done.finished"].Value : null
            });
        });

        staff.MapGet("/inspection/queue/{inspectionId:guid}", async (HttpContext http, Guid inspectionId, GetOperationsInspectionUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
            await WorkAsync(http, inspectionId, null, get, brand, errors));

        staff.MapPost("/inspection/queue/{inspectionId:guid}/start", async (HttpContext http, Guid inspectionId, [FromForm] string? key, StartOperationsInspectionUseCase start,
            GetOperationsInspectionUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            StartOperationsInspectionResponse response = await start.ExecuteAsync(OperationsCurrentUser.Context(http),
                new StartOperationsInspectionRequest { InspectionId = inspectionId, CommandKey = key ?? string.Empty }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/inspection/queue/" + inspectionId) : await WorkAsync(http, inspectionId, response.Error, get, brand, errors);
        });

        staff.MapPost("/inspection/queue/{inspectionId:guid}/finish", async (HttpContext http, Guid inspectionId, FinishOperationsInspectionUseCase finish, GetOperationsInspectionUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            List<OperationsParcelPhotoUpload> photos = new();

            foreach (IFormFile file in form.Files.GetFiles("photo_contents").Where(file => file.Length > 0).Take(OperationsReceivingLimits.MaxPhotosPerReceipt + 1))
            {
                if (file.Length > OperationsReceivingLimits.MaxPhotoBytes)
                {
                    return await WorkAsync(http, inspectionId, OperationsReceivingErrors.PhotoInvalid, get, brand, errors);
                }

                using MemoryStream buffer = new((int)file.Length);
                await file.CopyToAsync(buffer, http.RequestAborted);
                photos.Add(new OperationsParcelPhotoUpload("contents", buffer.ToArray()));
            }

            FinishOperationsInspectionResponse response = await finish.ExecuteAsync(OperationsCurrentUser.Context(http), new FinishOperationsInspectionRequest
            {
                InspectionId = inspectionId,
                Result = form["result"].ToString(),
                ItemIds = form["items"].Select(value => Guid.TryParse(value, out Guid id) ? id : Guid.Empty).Where(id => id != Guid.Empty).Take(OperationsInspectionLimits.MaxItems).ToList(),
                Notes = form["notes"].ToString(),
                CustomerAction = form["action"].ToString(),
                HoldReason = Choice(form["hold"].ToString(), out OperationsHoldReasons hold) ? hold : OperationsHoldReasons.None,
                Fee = Amount(form["fee"].ToString()),
                CalledCustomerUtc = DateOnly.TryParseExact(form["calledOn"].ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly day)
                    ? new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
                    : null,
                CallNotes = form["callNotes"].ToString(),
                Photos = photos,
                CommandKey = form["key"].ToString(),
                PortalSlug = OperationsStaffResults.PortalSlug(http)
            }, http.RequestAborted);

            return response.Succeeded ? Results.Redirect("/staff/inspection/queue?done=finished") : await WorkAsync(http, inspectionId, response.Error, get, brand, errors);
        }).WithMetadata(new OperationsUploadLimit(UploadLimit)).WithFormOptions(multipartBodyLengthLimit: UploadLimit);

        staff.MapGet("/holds", async (HttpContext http, string? reason, Guid? warehouse, int? page, string? done, ListOperationsHoldsUseCase list, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
            await HoldsAsync(http, reason, warehouse, page, null, done is "released" or "decided" or "reviewed" or "saved" ? text["ui.holds.done." + done].Value : null, list, brand, errors));

        // Slice 3E: the customer's answers, unclaimed packages and their number of days.
        staff.MapPost("/holds/answers/{answerId:guid}", async (HttpContext http, Guid answerId, [FromForm] string? decision, [FromForm] string? note, [FromForm] string? key,
            Vixly.Operations.Application.UseCases.Holds.ReviewOperationsHoldAnswer.ReviewOperationsHoldAnswerUseCase review, ListOperationsHoldsUseCase list, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
        {
            if (decision is not ("accept" or "decline"))
            {
                return await HoldsAsync(http, null, null, 1, Vixly.Operations.Domain.Holds.OperationsHoldErrors.DecisionInvalid, null, list, brand, errors);
            }

            Vixly.Operations.Application.UseCases.Holds.ReviewOperationsHoldAnswer.ReviewOperationsHoldAnswerResponse response = await review.ExecuteAsync(OperationsCurrentUser.Context(http),
                new Vixly.Operations.Application.UseCases.Holds.ReviewOperationsHoldAnswer.ReviewOperationsHoldAnswerRequest
                {
                    AnswerId = answerId,
                    Accept = decision == "accept",
                    Note = note,
                    CommandKey = key ?? string.Empty,
                    PortalSlug = OperationsStaffResults.PortalSlug(http)
                }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/holds?done=reviewed") : await HoldsAsync(http, null, null, 1, response.Error, null, list, brand, errors);
        });

        staff.MapGet("/holds/answers/{answerId:guid}/document", async (HttpContext http, Guid answerId,
            Vixly.Operations.Application.UseCases.Holds.GetOperationsHoldAnswerDocument.GetOperationsHoldAnswerDocumentUseCase get) =>
        {
            Vixly.Operations.Application.UseCases.Holds.GetOperationsHoldAnswerDocument.GetOperationsHoldAnswerDocumentResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http),
                new Vixly.Operations.Application.UseCases.Holds.GetOperationsHoldAnswerDocument.GetOperationsHoldAnswerDocumentRequest { AnswerId = answerId }, http.RequestAborted);

            if (!response.Succeeded)
            {
                return Results.NotFound();
            }

            // A download in a sandbox: nothing the customer sent runs in the staff site.
            http.Response.Headers.CacheControl = "private, no-store";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            http.Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
            return Results.File(response.Content, response.ContentType, response.FileName);
        });

        staff.MapPost("/holds/unclaimed/{parcelId:guid}", async (HttpContext http, Guid parcelId, [FromForm] string? decision, [FromForm] string? note, [FromForm] string? key,
            Vixly.Operations.Application.UseCases.Holds.DecideOperationsUnclaimed.DecideOperationsUnclaimedUseCase decide, ListOperationsHoldsUseCase list, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
        {
            Vixly.Operations.Application.UseCases.Holds.DecideOperationsUnclaimed.DecideOperationsUnclaimedResponse response = await decide.ExecuteAsync(OperationsCurrentUser.Context(http),
                new Vixly.Operations.Application.UseCases.Holds.DecideOperationsUnclaimed.DecideOperationsUnclaimedRequest
                {
                    ParcelId = parcelId,
                    Decision = decision ?? string.Empty,
                    Note = note,
                    CommandKey = key ?? string.Empty
                }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/holds?done=decided") : await HoldsAsync(http, null, null, 1, response.Error, null, list, brand, errors);
        });

        staff.MapPost("/holds/unclaimed-days", async (HttpContext http, [FromForm] string? days,
            Vixly.Operations.Application.UseCases.Holds.ChangeOperationsUnclaimedDays.ChangeOperationsUnclaimedDaysUseCase change, ListOperationsHoldsUseCase list, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
        {
            Vixly.Operations.Application.UseCases.Holds.ChangeOperationsUnclaimedDays.ChangeOperationsUnclaimedDaysResponse response = await change.ExecuteAsync(OperationsCurrentUser.Context(http),
                new Vixly.Operations.Application.UseCases.Holds.ChangeOperationsUnclaimedDays.ChangeOperationsUnclaimedDaysRequest
                {
                    Days = int.TryParse(days, NumberStyles.None, CultureInfo.InvariantCulture, out int value) ? value : -1
                }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/holds?done=saved") : await HoldsAsync(http, null, null, 1, response.Error, null, list, brand, errors);
        });

        // Slice 4D step 3: free days and the storage fee per day.
        staff.MapPost("/holds/storage-fee", async (HttpContext http, [FromForm] string? freeDays, [FromForm] string? fee,
            Vixly.Operations.Application.UseCases.Holds.ChangeOperationsStorageFee.ChangeOperationsStorageFeeUseCase change, ListOperationsHoldsUseCase list, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
        {
            string typed = (fee ?? string.Empty).Trim();
            decimal? perDay = typed.Length == 0 ? null
                : typed.Length <= 10 && typed.All(character => char.IsAsciiDigit(character) || character == '.') && decimal.TryParse(typed, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal value) ? value : -1m;
            Vixly.Operations.Application.UseCases.Holds.ChangeOperationsStorageFee.ChangeOperationsStorageFeeResponse response = await change.ExecuteAsync(OperationsCurrentUser.Context(http),
                new Vixly.Operations.Application.UseCases.Holds.ChangeOperationsStorageFee.ChangeOperationsStorageFeeRequest
                {
                    FreeDays = int.TryParse(freeDays, NumberStyles.None, CultureInfo.InvariantCulture, out int days) ? days : -1,
                    FeePerDay = perDay
                }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/holds?done=saved") : await HoldsAsync(http, null, null, 1, response.Error, null, list, brand, errors);
        });

        staff.MapPost("/holds/{parcelId:guid}/release", async (HttpContext http, Guid parcelId, [FromForm] string? reason, [FromForm] string? note, [FromForm] string? key,
            ReleaseOperationsHoldUseCase release, ListOperationsHoldsUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            ReleaseOperationsHoldResponse response = await release.ExecuteAsync(OperationsCurrentUser.Context(http), new ReleaseOperationsHoldRequest
            {
                ParcelId = parcelId,
                Reason = Choice(reason, out OperationsHoldReasons chosen) ? chosen : OperationsHoldReasons.None,
                Note = note,
                CommandKey = key ?? string.Empty
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/holds?done=released") : await HoldsAsync(http, null, null, 1, response.Error, null, list, brand, errors);
        });

        staff.MapPost("/holds/disposals/{inspectionId:guid}", async (HttpContext http, Guid inspectionId, [FromForm] string? decision, [FromForm] string? note, [FromForm] string? key,
            DecideOperationsDisposalUseCase decide, ListOperationsHoldsUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            if (decision is not ("approve" or "decline"))
            {
                return await HoldsAsync(http, null, null, 1, OperationsInspectionErrors.NotAwaitingApproval, null, list, brand, errors);
            }

            DecideOperationsDisposalResponse response = await decide.ExecuteAsync(OperationsCurrentUser.Context(http), new DecideOperationsDisposalRequest
            {
                InspectionId = inspectionId,
                Approve = decision == "approve",
                Note = note,
                CommandKey = key ?? string.Empty
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/holds?done=decided") : await HoldsAsync(http, null, null, 1, response.Error, null, list, brand, errors);
        });

        return endpoints;
    }

    private static async Task<IResult> SetupAsync(HttpContext http, Error? error, string? info, GetOperationsInspectionSetupUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsInspectionSetupResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsInspectionSetupRequest(), http.RequestAborted);
        return OperationsStaffResults.Page<InspectionSetupPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Items = response.Items,
            Rules = response.Rules,
            Percent = response.Percent,
            CurrencyCode = response.CurrencyCode,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }

    private static async Task<IResult> WorkAsync(HttpContext http, Guid inspectionId, Error? error, GetOperationsInspectionUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsInspectionResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsInspectionRequest { InspectionId = inspectionId }, http.RequestAborted);

        if (response.Inspection is not { } inspection || response.Parcel is not { } parcel)
        {
            return Results.NotFound();
        }

        return OperationsStaffResults.Page<InspectionWorkPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Inspection = inspection,
            Parcel = parcel,
            Shipment = response.Shipment,
            Applicable = response.Applicable,
            Named = response.Named,
            CanPerform = response.CanPerform,
            CurrencyCode = response.CurrencyCode,
            CommandKey = Guid.NewGuid().ToString("N"),
            Error = errors.Describe(error)
        });
    }

    private static async Task<IResult> HoldsAsync(HttpContext http, string? reason, Guid? warehouse, int? page, Error? error, string? info, ListOperationsHoldsUseCase list,
        GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        OperationsHoldReasons chosen = Choice(reason, out OperationsHoldReasons parsed) ? parsed : OperationsHoldReasons.None;
        ListOperationsHoldsResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http),
            new ListOperationsHoldsRequest { Reason = chosen, WarehouseId = warehouse, Page = Math.Max(page ?? 1, 1) }, http.RequestAborted);
        return OperationsStaffResults.Page<HoldsPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Reason = chosen,
            Items = response.Items,
            TotalCount = response.TotalCount,
            Page = Math.Max(page ?? 1, 1),
            Approvals = response.Approvals,
            CanApprove = response.CanApprove,
            Unclaimed = response.Unclaimed,
            UnclaimedCount = response.UnclaimedCount,
            UnclaimedDays = response.UnclaimedDays,
            StorageFreeDays = response.StorageFreeDays,
            StorageFeePerDay = response.StorageFeePerDay,
            CurrencyCode = response.CurrencyCode,
            Releasable = ReleaseOperationsHoldUseCase.Releasable,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }

    // A name of the enum only, never a number ("4" would read as a reason).
    private static bool Choice<TEnum>(string? value, out TEnum chosen)
        where TEnum : struct, Enum
    {
        chosen = default;
        return value is { Length: > 0 and <= 40 } && !value.Any(char.IsDigit) && Enum.TryParse(value, ignoreCase: false, out chosen) && Enum.IsDefined(chosen);
    }

    // Browsers send number fields with a dot; anything with a group separator is refused rather than guessed (review M3).
    private static decimal? Amount(string? value) =>
        value is { Length: > 0 and <= 20 } && value.Trim().All(character => char.IsAsciiDigit(character) || character == '.')
        && decimal.TryParse(value.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal number)
            ? number
            : null;
}
