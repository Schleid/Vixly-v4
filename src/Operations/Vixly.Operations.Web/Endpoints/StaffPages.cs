using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Platform;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Staff;
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
using Vixly.Operations.Web.Security;
using Vixly.Operations.Web.Tenancy;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: StaffPages.cs
//
// PURPOSE:
// Renders the staff, roles, account and confirm pages with their data (Slice 1D).
//
// RESPONSIBILITIES:
// - Load the data a page needs through the use cases
// - Turn error codes into text (L-1)
//
// IMPORTANT NOTES:
// - Scoped per request; pages are never cached (no-store)
// =========================================================
public sealed class StaffPages
{
    private readonly ListOperationsStaffUseCase _list;
    private readonly GetOperationsStaffMemberUseCase _member;
    private readonly ListOperationsRolesUseCase _roles;
    private readonly ListOperationsLocationsUseCase _locations;
    private readonly GetMyOperationsAccountUseCase _account;
    private readonly GetOperationsSignInBrandUseCase _brand;
    private readonly OperationsErrorText _errors;
    private readonly IStringLocalizer<OperationsText> _text;

    public StaffPages(
        ListOperationsStaffUseCase list,
        GetOperationsStaffMemberUseCase member,
        ListOperationsRolesUseCase roles,
        ListOperationsLocationsUseCase locations,
        GetMyOperationsAccountUseCase account,
        GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors,
        IStringLocalizer<OperationsText> text)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
        _member = member ?? throw new ArgumentNullException(nameof(member));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _account = account ?? throw new ArgumentNullException(nameof(account));
        _brand = brand ?? throw new ArgumentNullException(nameof(brand));
        _errors = errors ?? throw new ArgumentNullException(nameof(errors));
        _text = text ?? throw new ArgumentNullException(nameof(text));
    }

    public string? Done(string done) => done switch
    {
        "invited" or "granted" or "revoked" or "status" or "second_factor" or "sessions" or "transferred" or "saved" or "password" => _text["ui.staff.done." + done].Value,
        _ => null
    };

    public async Task<IResult> ListAsync(HttpContext http, string? search, int page, Error? error, string? info)
    {
        OperationsAuthorizationContext context = OperationsCurrentUser.Context(http);
        ListOperationsStaffResponse list = await _list.ExecuteAsync(context, new ListOperationsStaffRequest { Search = search, Page = page }, http.RequestAborted);
        ListOperationsRolesResponse roles = await _roles.ExecuteAsync(context, new ListOperationsRolesRequest(), http.RequestAborted);
        ListOperationsLocationsResponse locations = await _locations.ExecuteAsync(context, new ListOperationsLocationsRequest(), http.RequestAborted);

        return Page<StaffListPage>(http, new
        {
            Company = await CompanyAsync(http),
            Search = search,
            Members = list.Members,
            TotalCount = list.TotalCount,
            CanInvite = roles.Succeeded,
            Roles = roles.Roles,
            Branches = locations.Branches,
            Warehouses = locations.Warehouses,
            Error = _errors.Describe(error ?? list.Error),
            Info = info
        });
    }

    public async Task<IResult> MemberAsync(HttpContext http, Guid userId, Error? error, string? info)
    {
        OperationsAuthorizationContext context = OperationsCurrentUser.Context(http);
        GetOperationsStaffMemberResponse member = await _member.ExecuteAsync(context, new GetOperationsStaffMemberRequest { UserId = userId }, http.RequestAborted);
        if (member.Member is null)
        {
            return member.Error?.Code == OperationsAccessErrors.PermissionRequired.Code ? await ListAsync(http, null, 1, member.Error, null) : Results.NotFound();
        }

        ListOperationsRolesResponse roles = await _roles.ExecuteAsync(context, new ListOperationsRolesRequest(), http.RequestAborted);
        ListOperationsLocationsResponse locations = await _locations.ExecuteAsync(context, new ListOperationsLocationsRequest(), http.RequestAborted);

        return Page<StaffMemberPage>(http, new
        {
            Company = await CompanyAsync(http),
            Member = member.Member,
            IsMe = context.UserId == userId,
            ViewerIsOwner = http.User.FindFirst(OperationsClaimTypes.Owner)?.Value == "1",
            Roles = roles.Roles,
            Branches = locations.Branches,
            Warehouses = locations.Warehouses,
            Error = _errors.Describe(error),
            Info = info
        });
    }

    public async Task<IResult> AfterMemberActionAsync(HttpContext http, Guid userId, bool succeeded, Error? error, string done)
    {
        if (succeeded)
        {
            return Results.Redirect("/staff/people/" + userId + "?done=" + done);
        }

        return OperationsStaffEndpoints.StepUp(error, "/staff/people/" + userId) ?? await MemberAsync(http, userId, error, null);
    }

    public async Task<IResult> RolesAsync(HttpContext http, Error? error, string? info)
    {
        ListOperationsRolesResponse roles = await _roles.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsRolesRequest(), http.RequestAborted);

        return Page<RolesPage>(http, new
        {
            Company = await CompanyAsync(http),
            Roles = roles.Roles,
            Error = _errors.Describe(error ?? roles.Error),
            Info = info
        });
    }

    public async Task<IResult> RoleFormAsync(HttpContext http, Guid? roleId, string? name, IReadOnlyList<string> permissions, Error? error)
    {
        return Page<RoleFormPage>(http, new
        {
            Company = await CompanyAsync(http),
            RoleId = roleId,
            Name = name,
            Permissions = permissions,
            Error = _errors.Describe(error)
        });
    }

    public async Task<IResult> AccountAsync(HttpContext http, Error? error, string? info, IReadOnlyList<string>? newCodes)
    {
        GetMyOperationsAccountResponse account = await _account.ExecuteAsync(OperationsCurrentUser.Context(http), new GetMyOperationsAccountRequest(), http.RequestAborted);
        if (account.Me is null)
        {
            return Results.Redirect("/staff");
        }

        http.Response.Headers.CacheControl = "no-store";
        return Page<AccountPage>(http, new
        {
            Company = await CompanyAsync(http),
            Me = account.Me,
            RecoveryCodesLeft = account.RecoveryCodesLeft,
            Language = http.User.FindFirst(OperationsClaimTypes.Language)?.Value ?? "en",
            Sessions = account.Sessions,
            NewCodes = newCodes,
            Error = _errors.Describe(error ?? account.Error),
            Info = info
        });
    }

    public async Task<IResult> ConfirmAsync(HttpContext http, string returnUrl, Error? error)
    {
        GetMyOperationsAccountResponse account = await _account.ExecuteAsync(OperationsCurrentUser.Context(http), new GetMyOperationsAccountRequest(), http.RequestAborted);

        return Page<ConfirmPage>(http, new
        {
            Company = await CompanyAsync(http),
            ReturnUrl = returnUrl,
            HasSecondFactor = account.Me?.HasSecondFactor ?? false,
            Error = _errors.Describe(error)
        });
    }

    private static IResult Page<TComponent>(HttpContext http, object parameters)
        where TComponent : Microsoft.AspNetCore.Components.IComponent
    {
        http.Response.Headers.CacheControl = "no-store";
        return new RazorComponentResult<TComponent>(parameters);
    }

    private async Task<string?> CompanyAsync(HttpContext http)
    {
        GetOperationsSignInBrandResponse response = await _brand.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsSignInBrandRequest(), http.RequestAborted);
        return response.DisplayName;
    }
}
