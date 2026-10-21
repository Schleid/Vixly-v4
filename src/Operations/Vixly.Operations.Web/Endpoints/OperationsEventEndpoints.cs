using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.UseCases.Events.FindOperationsEventSubject;
using Vixly.Operations.Application.UseCases.Events.GetOperationsEventTimeline;
using Vixly.Operations.Application.UseCases.Events.AddOperationsEvent;
using Vixly.Operations.Application.UseCases.Events.VoidOperationsEvent;
using Vixly.Operations.Application.UseCases.Events.GetOperationsEventSettings;
using Vixly.Operations.Application.UseCases.Events.SaveOperationsEventSettings;
using Vixly.Operations.Application.UseCases.Events.SaveOperationsEventReason;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Domain.Events;
using Vixly.Operations.Domain.Tracking;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Events;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Localization;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsEventEndpoints.cs
//
// PURPOSE:
// Operational events (Slice 6C): the events workspace, a subject's history, recording and voiding by hand, Settings → Events.
//
// RESPONSIBILITIES:
// - /staff/events and /staff/events/{kind}/{id}; /staff/events/void/{eventId}; /staff/settings/events
//
// IMPORTANT NOTES:
// - Static forms with antiforgery and the form guard (ADR-0019); the use cases check every permission
// =========================================================
public static class OperationsEventEndpoints
{
    public static IEndpointRouteBuilder MapOperationsEventEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        staff.MapGet("/events", async (HttpContext http, GetOperationsSignInBrandUseCase brand) =>
            OperationsStaffResults.Page<EventsPage>(http, new { Company = await OperationsStaffResults.CompanyAsync(http, brand) }));

        staff.MapPost("/events", async (HttpContext http, [FromForm] string? query, FindOperationsEventSubjectUseCase find, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            FindOperationsEventSubjectResponse response = await find.ExecuteAsync(OperationsCurrentUser.Context(http), new FindOperationsEventSubjectRequest { Query = query }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/events/" + response.Kind + "/" + response.Id)
                : OperationsStaffResults.Page<EventsPage>(http, new { Company = await OperationsStaffResults.CompanyAsync(http, brand), Query = query, Error = errors.Describe(response.Error) });
        });

        staff.MapGet("/events/{kind}/{id:guid}", async (HttpContext http, string kind, Guid id, string? filter, int? page, string? done, GetOperationsEventTimelineUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors, IStringLocalizer<OperationsText> texts) =>
            await TimelineAsync(http, kind, id, filter, page ?? 1, null, done is "recorded" or "voided" ? texts["ui.events.done." + done].Value : null, get, brand, errors));

        staff.MapPost("/events/{kind}/{id:guid}", async (HttpContext http, string kind, Guid id, [FromForm] string? eventKind, [FromForm] string? cause, [FromForm] string? message,
            [FromForm] string? note, [FromForm] string? at, [FromForm] string? key, AddOperationsEventUseCase add, GetOperationsEventTimelineUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
        {
            if (!Subject(kind, out OperationsTrackingSubject subject))
            {
                return Results.NotFound();
            }

            AddOperationsEventResponse response = await add.ExecuteAsync(OperationsCurrentUser.Context(http), new AddOperationsEventRequest
            {
                Kind = subject,
                Id = id,
                EventKind = Enum.TryParse(eventKind, false, out OperationsEventKind parsed) && Enum.IsDefined(parsed) ? parsed : (OperationsEventKind)0,
                Cause = cause,
                Message = message,
                Note = note,
                At = OperationsDispatchEndpoints.Local(at),
                RequestKey = Guid.TryParseExact(key, "N", out Guid formKey) ? formKey : null,
                PortalSlug = OperationsStaffResults.PortalSlug(http)
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/events/" + subject + "/" + id + "?done=recorded") : await TimelineAsync(http, kind, id, null, 1, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/events/void/{eventId:guid}", async (HttpContext http, Guid eventId, [FromForm] string? reason, VoidOperationsEventUseCase voiding, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
        {
            VoidOperationsEventResponse response = await voiding.ExecuteAsync(OperationsCurrentUser.Context(http), new VoidOperationsEventRequest { EventId = eventId, Reason = reason }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/events/" + response.Kind + "/" + response.SubjectId + "?done=voided")
                : OperationsStaffResults.Page<EventsPage>(http, new { Company = await OperationsStaffResults.CompanyAsync(http, brand), Error = errors.Describe(response.Error) });
        });

        staff.MapGet("/settings/events", async (HttpContext http, string? done, GetOperationsEventSettingsUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors,
            IStringLocalizer<OperationsText> texts) =>
            await SettingsAsync(http, null, done == "1" ? texts["ui.staff.done.saved"].Value : null, get, brand, errors));

        staff.MapPost("/settings/events", async (HttpContext http, [FromForm] bool? emails, [FromForm] bool? pickupEmails, SaveOperationsEventSettingsUseCase save, GetOperationsEventSettingsUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            HashSet<string> enabled = (await http.Request.ReadFormAsync(http.RequestAborted))["enabled"].Where(value => value is not null).Select(value => value!).ToHashSet(StringComparer.Ordinal);
            SaveOperationsEventSettingsResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), new SaveOperationsEventSettingsRequest
            {
                EventEmails = emails == true,
                PickupEmails = pickupEmails == true,
                DisabledCauses = OperationsEventReasons.Delay.Concat(OperationsEventReasons.Exception).Where(code => !enabled.Contains(code)).ToList()
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/settings/events?done=1") : await SettingsAsync(http, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/settings/events/causes", async (HttpContext http, [FromForm] string? kind, [FromForm] string? en, [FromForm] string? fr, [FromForm] string? es,
            SaveOperationsEventReasonUseCase save, GetOperationsEventSettingsUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            SaveOperationsEventReasonResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), Reason(null, kind, en, fr, es, true), http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/settings/events?done=1") : await SettingsAsync(http, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/settings/events/causes/{reasonId:guid}", async (HttpContext http, Guid reasonId, [FromForm] string? kind, [FromForm] string? en, [FromForm] string? fr, [FromForm] string? es,
            [FromForm] bool? active, SaveOperationsEventReasonUseCase save, GetOperationsEventSettingsUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            SaveOperationsEventReasonResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), Reason(reasonId, kind, en, fr, es, active == true), http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/settings/events?done=1") : await SettingsAsync(http, response.Error, null, get, brand, errors);
        });

        return endpoints;
    }

    private static bool Subject(string kind, out OperationsTrackingSubject subject) =>
        Enum.TryParse(kind, false, out subject) && subject is OperationsTrackingSubject.Parcel or OperationsTrackingSubject.Shipment or OperationsTrackingSubject.Bundle or OperationsTrackingSubject.Dispatch;

    private static SaveOperationsEventReasonRequest Reason(Guid? reasonId, string? kind, string? en, string? fr, string? es, bool active) => new()
    {
        ReasonId = reasonId,
        Kind = Enum.TryParse(kind, false, out OperationsEventKind parsed) && Enum.IsDefined(parsed) ? parsed : (OperationsEventKind)0,
        TextEn = en,
        TextFr = fr,
        TextEs = es,
        Active = active
    };

    private static async Task<IResult> TimelineAsync(HttpContext http, string kind, Guid id, string? filter, int page, Error? error, string? info, GetOperationsEventTimelineUseCase get,
        GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        if (!Subject(kind, out OperationsTrackingSubject subject))
        {
            return Results.NotFound();
        }

        string shown = filter is "customer" or "manual" or "voided" ? filter : "all";
        GetOperationsEventTimelineResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsEventTimelineRequest { Kind = subject, Id = id, Filter = shown, Page = page },
            http.RequestAborted);

        if (!response.Succeeded)
        {
            return response.Error?.Code == OperationsEventErrors.NotFound.Code
                ? Results.NotFound()
                : OperationsStaffResults.Page<EventsPage>(http, new { Company = await OperationsStaffResults.CompanyAsync(http, brand), Error = errors.Describe(response.Error) });
        }

        return OperationsStaffResults.Page<EventTimelinePage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.Subject,
            response.Lines,
            response.Pages,
            Page = Math.Clamp(page, 1, response.Pages),
            Filter = shown,
            response.Parcels,
            response.CanAdd,
            response.DelayCauses,
            response.ExceptionCauses,
            response.TimeZone,
            Error = errors.Describe(error),
            Info = info
        });
    }

    private static async Task<IResult> SettingsAsync(HttpContext http, Error? error, string? info, GetOperationsEventSettingsUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        GetOperationsEventSettingsResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsEventSettingsRequest(), http.RequestAborted);
        return OperationsStaffResults.Page<EventSettingsPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.EventEmails,
            response.PickupEmails,
            response.DisabledCauses,
            response.Reasons,
            response.CanEdit,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }
}
