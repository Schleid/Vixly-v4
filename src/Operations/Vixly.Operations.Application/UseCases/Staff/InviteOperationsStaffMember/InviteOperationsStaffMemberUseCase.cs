using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Staff;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.UseCases.Staff.InviteOperationsStaffMember;

// =========================================================
// FILE: InviteOperationsStaffMemberUseCase.cs
//
// PURPOSE:
// Invites a new staff member with a first role and emails the 72-hour setup link (Slice 1D).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.staff.invite and ops.roles.grant, and the inviter must hold everything the role gives in that scope
// - Only the Owner gives the Administrator role
// - The email is the same setup email as the Owner's (password, then the second factor when the role needs one)
// =========================================================
public sealed class InviteOperationsStaffMemberUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsUserRepository _users;

    private readonly IOperationsRoleRepository _roles;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsAccountTokenRepository _tokens;

    private readonly IOperationsSecretGenerator _secrets;

    private readonly IOperationsBrandRepository _brands;

    private readonly IOperationsIdentityMailer _mailer;

    private readonly IOperationsPortalLinks _links;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public InviteOperationsStaffMemberUseCase(
        OperationsAccess access,
        IOperationsUserRepository users,
        IOperationsRoleRepository roles,
        IOperationsLocationRepository locations,
        IOperationsAccountTokenRepository tokens,
        IOperationsSecretGenerator secrets,
        IOperationsBrandRepository brands,
        IOperationsIdentityMailer mailer,
        IOperationsPortalLinks links,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
        _links = links ?? throw new ArgumentNullException(nameof(links));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<InviteOperationsStaffMemberResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        InviteOperationsStaffMemberRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.StaffInvite, true, cancellationToken);

        if (refused is not null)
        {
            return InviteOperationsStaffMemberResponse.Failure(refused);
        }

        if (!EmailAddress.TryCreate(request.Email, out EmailAddress? email) || !LanguageCode.TryCreate(request.Language, out LanguageCode? language))
        {
            return InviteOperationsStaffMemberResponse.Failure(OperationsCommonErrors.RequestInvalid);
        }

        if (await _users.FindByEmailAsync(email!, cancellationToken) is not null)
        {
            return InviteOperationsStaffMemberResponse.Failure(OperationsStaffErrors.EmailTaken);
        }

        OperationsRole? role = await _roles.GetAsync(request.RoleId, cancellationToken);
        Guid? branchOfWarehouse = request.Scope == OperationsRoleScope.Warehouse && request.ScopeId is { } warehouseId
            ? (await _locations.GetWarehouseAsync(warehouseId, cancellationToken))?.BranchId
            : null;

        if (role is null || !role.IsActive)
        {
            return InviteOperationsStaffMemberResponse.Failure(OperationsRoleErrors.RoleInactive);
        }

        // Inviting gives a role: the same scope rules as a grant (GrantOperationsRole).
        if ((request.Scope == OperationsRoleScope.Branch && (request.ScopeId is not { } branchId || await _locations.GetBranchAsync(branchId, cancellationToken) is null))
            || (request.Scope == OperationsRoleScope.Warehouse && branchOfWarehouse is null)
            || (role.IsAdministrator && request.Scope != OperationsRoleScope.Tenant))
        {
            return InviteOperationsStaffMemberResponse.Failure(OperationsRoleErrors.ScopeInvalid);
        }

        if (!access!.HasTenantWide(OperationsPermissions.RolesGrant) || !access.CanGrant(role, request.Scope, request.ScopeId, branchOfWarehouse))
        {
            return InviteOperationsStaffMemberResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsUser user;
        OperationsSecret secret = _secrets.NewToken();

        try
        {
            user = OperationsUser.InviteStaff(context.RequiredTenantId, request.FullName, email!, language!, utcNow);
            _users.Add(user);
            _roles.Add(OperationsRoleGrant.Grant(context.RequiredTenantId, user.Id, role.Id, request.Scope, request.ScopeId, access.User.Id, utcNow));
            user.SetAdministrator(role.IsAdministrator, utcNow);
            _tokens.Add(OperationsAccountToken.Issue(context.RequiredTenantId, user.Id, OperationsAccountTokenPurpose.Invitation, secret.Hash, utcNow));
        }
        catch (DomainException exception)
        {
            return InviteOperationsStaffMemberResponse.Failure(exception.Error);
        }

        _audit.Record(access.User.Id, "ops.staff.invited", nameof(OperationsUser), user.Id, arguments: new { role = role.Id, scope = request.Scope.ToString() });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return InviteOperationsStaffMemberResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
        }

        OperationsTenantBrand? brand = await _brands.FindAsync(cancellationToken);
        await _mailer.SendOwnerInvitationAsync(
            new OperationsOwnerInvitationMail(user.Email.Value, user.FullName, user.PreferredLanguage, brand?.DisplayName ?? string.Empty,
                _links.Page(request.PortalSlug, "/invitation?token=" + Uri.EscapeDataString(secret.Value)), (int)OperationsIdentityLimits.InvitationLifetime.TotalHours),
            cancellationToken);

        return InviteOperationsStaffMemberResponse.Success(user.Id);
    }
}
