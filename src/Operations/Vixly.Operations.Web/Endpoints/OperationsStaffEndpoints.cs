using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Staff;
using Vixly.Operations.Application.UseCases.Account.ChangeMyOperationsLanguage;
using Vixly.Operations.Application.UseCases.Account.ChangeMyOperationsPassword;
using Vixly.Operations.Application.UseCases.Account.ConfirmOperationsSecondFactor;
using Vixly.Operations.Application.UseCases.Account.EndMyOperationsSession;
using Vixly.Operations.Application.UseCases.Account.GetMyOperationsAccount;
using Vixly.Operations.Application.UseCases.Account.RegenerateMyRecoveryCodes;
using Vixly.Operations.Application.UseCases.Locations.ListOperationsLocations;
using Vixly.Operations.Application.UseCases.Roles.GrantOperationsRole;
using Vixly.Operations.Application.UseCases.Roles.ListOperationsRoles;
using Vixly.Operations.Application.UseCases.Roles.RevokeOperationsRoleGrant;
using Vixly.Operations.Application.UseCases.Roles.SaveOperationsRole;
using Vixly.Operations.Application.UseCases.Roles.SetOperationsRoleActive;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Application.UseCases.Staff.ChangeOperationsStaffStatus;
using Vixly.Operations.Application.UseCases.Staff.EndOperationsStaffSessions;
using Vixly.Operations.Application.UseCases.Staff.GetOperationsStaffMember;
using Vixly.Operations.Application.UseCases.Staff.InviteOperationsStaffMember;
using Vixly.Operations.Application.UseCases.Staff.ListOperationsStaff;
using Vixly.Operations.Application.UseCases.Staff.ResetOperationsStaffSecondFactor;
using Vixly.Operations.Application.UseCases.Staff.TransferOperationsOwnership;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Staff;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Security;
using Vixly.Operations.Web.Tenancy;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsStaffEndpoints.cs
//
// PURPOSE:
// Staff, roles, my account and the step-up page (Slice 1D).
//
// RESPONSIBILITIES:
// - /staff/people: list, invite, roles and security actions for one person
// - /staff/roles: the roles of the tenant and their permissions
// - /staff/account: my password, sessions and recovery codes
// - /staff/confirm: confirm a code before a sensitive action
//
// IMPORTANT NOTES:
// - Static forms with antiforgery (ADR-0019); the use cases check permissions and step-up
// - Rendering lives in StaffPages
// =========================================================
public static class OperationsStaffEndpoints
{
    public static IEndpointRouteBuilder MapOperationsStaffEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // Every change under /staff checks the antiforgery token, also when the form carries no other field (ADR-0019).
        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        // People
        staff.MapGet("/people", async (HttpContext http, string? search, int? page, string? done, StaffPages pages) =>
            await pages.ListAsync(http, search, page ?? 1, null, Done(pages, done)));

        staff.MapPost("/people/invite", async (HttpContext http, [FromForm] string? fullName, [FromForm] string? email, [FromForm] string? language, [FromForm] Guid roleId, [FromForm] string? scope,
            InviteOperationsStaffMemberUseCase useCase, StaffPages pages) =>
        {
            (OperationsRoleScope kind, Guid? scopeId) = Scope(scope);
            InviteOperationsStaffMemberResponse response = await Unavailable(() => useCase.ExecuteAsync(OperationsCurrentUser.Context(http), new InviteOperationsStaffMemberRequest
            {
                FullName = fullName ?? string.Empty,
                Email = email ?? string.Empty,
                Language = language ?? "en",
                RoleId = roleId,
                Scope = kind,
                ScopeId = scopeId,
                PortalSlug = OperationsTenantMiddleware.Host(http)?.CanonicalSlug ?? string.Empty
            }, http.RequestAborted));

            return response.Succeeded
                ? Results.Redirect("/staff/people/" + response.UserId + "?done=invited")
                : StepUp(response.Error, "/staff/people") ?? await pages.ListAsync(http, null, 1, response.Error, null);
        });

        // Slice 7 step 4 (R-8): a partner agent for a partner-operated warehouse.
        staff.MapPost("/people/invite-partner", async (HttpContext http, [FromForm] string? fullName, [FromForm] string? email, [FromForm] string? language, [FromForm] Guid warehouseId,
            InviteOperationsStaffMemberUseCase useCase, StaffPages pages) =>
        {
            InviteOperationsStaffMemberResponse response = await Unavailable(() => useCase.ExecuteAsync(OperationsCurrentUser.Context(http), new InviteOperationsStaffMemberRequest
            {
                FullName = fullName ?? string.Empty,
                Email = email ?? string.Empty,
                Language = language ?? "en",
                PartnerWarehouseId = warehouseId,
                PortalSlug = OperationsTenantMiddleware.Host(http)?.CanonicalSlug ?? string.Empty
            }, http.RequestAborted));

            return response.Succeeded
                ? Results.Redirect("/staff/people/" + response.UserId + "?done=invited")
                : StepUp(response.Error, "/staff/people") ?? await pages.ListAsync(http, null, 1, response.Error, null);
        });

        staff.MapGet("/people/{userId:guid}", async (HttpContext http, Guid userId, string? done, StaffPages pages) =>
            await pages.MemberAsync(http, userId, null, Done(pages, done)));

        staff.MapPost("/people/{userId:guid}/grant", async (HttpContext http, Guid userId, [FromForm] Guid roleId, [FromForm] string? scope, GrantOperationsRoleUseCase useCase, StaffPages pages) =>
        {
            (OperationsRoleScope kind, Guid? scopeId) = Scope(scope);
            GrantOperationsRoleResponse response = await useCase.ExecuteAsync(OperationsCurrentUser.Context(http),
                new GrantOperationsRoleRequest { UserId = userId, RoleId = roleId, Scope = kind, ScopeId = scopeId }, http.RequestAborted);
            return await pages.AfterMemberActionAsync(http, userId, response.Succeeded, response.Error, "granted");
        });

        staff.MapPost("/people/{userId:guid}/revoke/{grantId:guid}", async (HttpContext http, Guid userId, Guid grantId, RevokeOperationsRoleGrantUseCase useCase, StaffPages pages) =>
        {
            RevokeOperationsRoleGrantResponse response = await useCase.ExecuteAsync(OperationsCurrentUser.Context(http), new RevokeOperationsRoleGrantRequest { GrantId = grantId, UserId = userId }, http.RequestAborted);
            return await pages.AfterMemberActionAsync(http, userId, response.Succeeded, response.Error, "revoked");
        });

        staff.MapPost("/people/{userId:guid}/status", async (HttpContext http, Guid userId, [FromForm] string? status, [FromForm] string? reason, ChangeOperationsStaffStatusUseCase useCase, StaffPages pages) =>
        {
            OperationsUserAccountStatus target = Enum.TryParse(status, ignoreCase: false, out OperationsUserAccountStatus parsed) && Enum.IsDefined(parsed) ? parsed : OperationsUserAccountStatus.Invited;
            ChangeOperationsStaffStatusResponse response = await useCase.ExecuteAsync(OperationsCurrentUser.Context(http),
                new ChangeOperationsStaffStatusRequest { UserId = userId, Status = target, Reason = reason ?? string.Empty }, http.RequestAborted);
            return await pages.AfterMemberActionAsync(http, userId, response.Succeeded, response.Error, "status");
        });

        // Owner, 2026-10-29 (S-5): an outside account (a call centre), with its end date, days and hours; or an ordinary one again.
        staff.MapPost("/people/{userId:guid}/outside", async (HttpContext http, Guid userId, [FromForm] bool outside, [FromForm] DateOnly? endsOn, [FromForm] int[]? days,
            [FromForm] TimeOnly? from, [FromForm] TimeOnly? to, Vixly.Operations.Application.UseCases.Staff.SetOperationsStaffOutside.SetOperationsStaffOutsideUseCase useCase, StaffPages pages) =>
        {
            // 24:00 is typed as 00:00 at the end of a shift: the whole day to midnight.
            int? fromMinute = from is { } start ? (start.Hour * 60) + start.Minute : null;
            int? toMinute = to is { } end ? (end == TimeOnly.MinValue && fromMinute is not null ? 24 * 60 : (end.Hour * 60) + end.Minute) : null;
            Vixly.Operations.Application.UseCases.Staff.SetOperationsStaffOutside.SetOperationsStaffOutsideResponse response = await useCase.ExecuteAsync(OperationsCurrentUser.Context(http),
                new Vixly.Operations.Application.UseCases.Staff.SetOperationsStaffOutside.SetOperationsStaffOutsideRequest
                {
                    UserId = userId, Outside = outside, EndsOn = endsOn, Days = (days ?? Array.Empty<int>()).Where(day => day is >= 1 and <= 64 && (day & (day - 1)) == 0).Distinct().Sum(),
                    FromMinute = fromMinute, ToMinute = toMinute
                }, http.RequestAborted);
            return await pages.AfterMemberActionAsync(http, userId, response.Succeeded, response.Error, "outside");
        });

        staff.MapPost("/people/{userId:guid}/reset-second-factor", async (HttpContext http, Guid userId, ResetOperationsStaffSecondFactorUseCase useCase, StaffPages pages) =>
        {
            ResetOperationsStaffSecondFactorResponse response = await useCase.ExecuteAsync(OperationsCurrentUser.Context(http), new ResetOperationsStaffSecondFactorRequest { UserId = userId }, http.RequestAborted);
            return await pages.AfterMemberActionAsync(http, userId, response.Succeeded, response.Error, "second_factor");
        });

        staff.MapPost("/people/{userId:guid}/end-sessions", async (HttpContext http, Guid userId, EndOperationsStaffSessionsUseCase useCase, StaffPages pages) =>
        {
            EndOperationsStaffSessionsResponse response = await useCase.ExecuteAsync(OperationsCurrentUser.Context(http), new EndOperationsStaffSessionsRequest { UserId = userId }, http.RequestAborted);
            return await pages.AfterMemberActionAsync(http, userId, response.Succeeded, response.Error, "sessions");
        });

        // Slice 12A (owner H-8): print a new badge (the old one stops working), or revoke it.
        staff.MapPost("/people/{userId:guid}/badge", async (HttpContext http, Guid userId,
            Vixly.Operations.Application.UseCases.Staff.IssueOperationsStaffBadge.IssueOperationsStaffBadgeUseCase useCase,
            Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand.GetOperationsSignInBrandUseCase brand,
            Vixly.Operations.Application.Ports.IOperationsLabelRenderer renderer, StaffPages pages) =>
        {
            Vixly.Operations.Application.UseCases.Staff.IssueOperationsStaffBadge.IssueOperationsStaffBadgeResponse response = await useCase.ExecuteAsync(OperationsCurrentUser.Context(http),
                new Vixly.Operations.Application.UseCases.Staff.IssueOperationsStaffBadge.IssueOperationsStaffBadgeRequest { UserId = userId }, http.RequestAborted);

            if (!response.Succeeded)
            {
                return await pages.AfterMemberActionAsync(http, userId, false, response.Error, "badge");
            }

            string company = (await brand.ExecuteAsync(OperationsCurrentUser.Context(http), new Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand.GetOperationsSignInBrandRequest(),
                http.RequestAborted)).DisplayName ?? string.Empty;
            http.Response.Headers.CacheControl = "no-store";
            return Results.File(renderer.RenderStaffBadge(new Vixly.Operations.Application.Identity.OperationsStaffBadgeCard(company, response.PersonName, response.Code)),
                "application/pdf", "badge.pdf");
        });

        staff.MapPost("/people/{userId:guid}/badge/revoke", async (HttpContext http, Guid userId,
            Vixly.Operations.Application.UseCases.Staff.RevokeOperationsStaffBadge.RevokeOperationsStaffBadgeUseCase useCase, StaffPages pages) =>
        {
            Vixly.Operations.Application.UseCases.Staff.RevokeOperationsStaffBadge.RevokeOperationsStaffBadgeResponse response = await useCase.ExecuteAsync(OperationsCurrentUser.Context(http),
                new Vixly.Operations.Application.UseCases.Staff.RevokeOperationsStaffBadge.RevokeOperationsStaffBadgeRequest { UserId = userId }, http.RequestAborted);
            return await pages.AfterMemberActionAsync(http, userId, response.Succeeded, response.Error, "badge_revoked");
        });

        staff.MapPost("/people/{userId:guid}/transfer", async (HttpContext http, Guid userId, [FromForm] bool confirm, TransferOperationsOwnershipUseCase useCase, StaffPages pages) =>
        {
            if (!confirm)
            {
                return Results.Redirect("/staff/people/" + userId);
            }

            TransferOperationsOwnershipResponse response = await useCase.ExecuteAsync(OperationsCurrentUser.Context(http), new TransferOperationsOwnershipRequest { UserId = userId }, http.RequestAborted);
            return await pages.AfterMemberActionAsync(http, userId, response.Succeeded, response.Error, "transferred");
        });

        // Roles
        staff.MapGet("/roles", async (HttpContext http, string? done, StaffPages pages) => await pages.RolesAsync(http, null, Done(pages, done)));

        staff.MapGet("/roles/new", async (HttpContext http, StaffPages pages) => await pages.RoleFormAsync(http, null, null, Array.Empty<string>(), null));

        staff.MapGet("/roles/{roleId:guid}", async (HttpContext http, Guid roleId, ListOperationsRolesUseCase list, StaffPages pages) =>
        {
            ListOperationsRolesResponse roles = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsRolesRequest(), http.RequestAborted);
            OperationsRoleSummary? role = roles.Roles.FirstOrDefault(item => item.RoleId == roleId && !item.IsAdministrator);
            return role is null
                ? (roles.Succeeded ? Results.NotFound() : await pages.RolesAsync(http, roles.Error, null))
                : await pages.RoleFormAsync(http, role.RoleId, role.Name, role.Permissions, null);
        });

        staff.MapPost("/roles/save", async (HttpContext http, [FromForm] Guid? roleId, [FromForm] string? name, SaveOperationsRoleUseCase useCase, StaffPages pages) =>
        {
            // The checked boxes arrive as one repeated field.
            string[] codes = http.Request.Form["permissions"].Where(code => !string.IsNullOrEmpty(code)).Select(code => code!).ToArray();
            SaveOperationsRoleResponse response = await useCase.ExecuteAsync(OperationsCurrentUser.Context(http),
                new SaveOperationsRoleRequest { RoleId = roleId, Name = name ?? string.Empty, Permissions = codes }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/roles?done=saved")
                : StepUp(response.Error, "/staff/roles") ?? await pages.RoleFormAsync(http, roleId, name, codes, response.Error);
        });

        staff.MapPost("/roles/{roleId:guid}/active", async (HttpContext http, Guid roleId, [FromForm] bool active, SetOperationsRoleActiveUseCase useCase, StaffPages pages) =>
        {
            SetOperationsRoleActiveResponse response = await useCase.ExecuteAsync(OperationsCurrentUser.Context(http), new SetOperationsRoleActiveRequest { RoleId = roleId, Active = active }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/roles?done=saved")
                : StepUp(response.Error, "/staff/roles") ?? await pages.RolesAsync(http, response.Error, null);
        });

        // My account
        staff.MapGet("/account", async (HttpContext http, string? done, StaffPages pages) => await pages.AccountAsync(http, null, Done(pages, done), null));

        staff.MapPost("/account/password", async (HttpContext http, [FromForm] string? currentPassword, [FromForm] string? newPassword, ChangeMyOperationsPasswordUseCase useCase, StaffPages pages) =>
        {
            ChangeMyOperationsPasswordResponse response = await useCase.ExecuteAsync(OperationsCurrentUser.Context(http),
                new ChangeMyOperationsPasswordRequest { CurrentPassword = currentPassword ?? string.Empty, NewPassword = newPassword ?? string.Empty }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/account?done=password") : await pages.AccountAsync(http, response.Error, null, null);
        });

        // Slice 12A: my PIN for licensed devices.
        staff.MapPost("/account/pin", async (HttpContext http, [FromForm] string? currentPassword, [FromForm] string? pin,
            Vixly.Operations.Application.UseCases.Account.SetMyOperationsPin.SetMyOperationsPinUseCase useCase, StaffPages pages) =>
        {
            Vixly.Operations.Application.UseCases.Account.SetMyOperationsPin.SetMyOperationsPinResponse response = await useCase.ExecuteAsync(OperationsCurrentUser.Context(http),
                new Vixly.Operations.Application.UseCases.Account.SetMyOperationsPin.SetMyOperationsPinRequest { CurrentPassword = currentPassword, Pin = pin }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/account?done=pin") : await pages.AccountAsync(http, response.Error, null, null);
        });

        staff.MapPost("/account/language", async (HttpContext http, [FromForm] string? language, ChangeMyOperationsLanguageUseCase useCase, StaffPages pages) =>
        {
            ChangeMyOperationsLanguageResponse response = await useCase.ExecuteAsync(OperationsCurrentUser.Context(http), new ChangeMyOperationsLanguageRequest { Language = language ?? "en" }, http.RequestAborted);

            if (!response.Succeeded)
            {
                return await pages.AccountAsync(http, response.Error, null, null);
            }

            // The pages follow at once; the emails follow the saved preference.
            OperationsCookies.Write(http.Response, OperationsCookies.Culture,
                Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.MakeCookieValue(new Microsoft.AspNetCore.Localization.RequestCulture(language!)), TimeSpan.FromDays(365));
            return Results.Redirect("/staff/account?done=saved");
        });

        staff.MapPost("/account/sessions/{sessionId:guid}/end", async (HttpContext http, Guid sessionId, EndMyOperationsSessionUseCase useCase, StaffPages pages) =>
        {
            EndMyOperationsSessionResponse response = await useCase.ExecuteAsync(OperationsCurrentUser.Context(http), new EndMyOperationsSessionRequest { SessionId = sessionId }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/account?done=sessions") : await pages.AccountAsync(http, response.Error, null, null);
        });

        staff.MapPost("/account/recovery-codes", async (HttpContext http, RegenerateMyRecoveryCodesUseCase useCase, StaffPages pages) =>
        {
            RegenerateMyRecoveryCodesResponse response = await useCase.ExecuteAsync(OperationsCurrentUser.Context(http), new RegenerateMyRecoveryCodesRequest(), http.RequestAborted);
            http.Response.Headers.CacheControl = "no-store";
            return response.Succeeded
                ? await pages.AccountAsync(http, null, null, response.Codes)
                : StepUp(response.Error, "/staff/account") ?? await pages.AccountAsync(http, response.Error, null, null);
        });

        // Step-up
        staff.MapGet("/confirm", async (HttpContext http, string? returnUrl, StaffPages pages) => await pages.ConfirmAsync(http, Local(returnUrl), null));

        staff.MapPost("/confirm", async (HttpContext http, [FromForm] string? code, [FromForm] string? returnUrl, ConfirmOperationsSecondFactorUseCase useCase, StaffPages pages) =>
        {
            ConfirmOperationsSecondFactorResponse response = await useCase.ExecuteAsync(OperationsCurrentUser.Context(http), new ConfirmOperationsSecondFactorRequest { Code = code ?? string.Empty }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect(Local(returnUrl)) : await pages.ConfirmAsync(http, Local(returnUrl), response.Error);
        }).RequireRateLimiting(OperationsRateLimits.SecondFactor);

        return endpoints;
    }

    // "tenant", "branch:{id}" or "warehouse:{id}" from the scope list.
    internal static (OperationsRoleScope Scope, Guid? ScopeId) Scope(string? value)
    {
        string[] parts = (value ?? "tenant").Split(':', 2);
        Guid? id = parts.Length == 2 && Guid.TryParse(parts[1], out Guid parsed) ? parsed : null;

        return parts[0] switch
        {
            "branch" when id is not null => (OperationsRoleScope.Branch, id),
            "warehouse" when id is not null => (OperationsRoleScope.Warehouse, id),
            _ => (OperationsRoleScope.Tenant, null)
        };
    }

    // A sensitive action without a fresh second factor goes to the confirm page, then back (D-O7).
    internal static IResult? StepUp(Error? error, string returnUrl) =>
        error?.Code == OperationsAccessErrors.StepUpRequired.Code ? Results.Redirect("/staff/confirm?returnUrl=" + Uri.EscapeDataString(returnUrl)) : null;

    // Only local staff addresses: never an open redirect.
    internal static string Local(string? returnUrl) =>
        returnUrl is { Length: > 1 } && returnUrl.StartsWith("/staff", StringComparison.Ordinal) && !returnUrl.StartsWith("//", StringComparison.Ordinal) && !returnUrl.Contains('\\', StringComparison.Ordinal)
            ? returnUrl
            : "/staff";

    private static string? Done(StaffPages pages, string? done) => done is null ? null : pages.Done(done);

    private static async Task<T> Unavailable<T>(Func<Task<T>> call)
    {
        try
        {
            return await call();
        }
        catch (OperationsPlatformUnavailableException exception)
        {
            throw new BadHttpRequestException("Platform is not available.", StatusCodes.Status503ServiceUnavailable, exception);
        }
    }
}
