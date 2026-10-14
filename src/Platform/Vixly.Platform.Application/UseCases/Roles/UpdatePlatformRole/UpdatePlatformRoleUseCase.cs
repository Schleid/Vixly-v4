using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Roles.UpdatePlatformRole;

// =========================================================
// FILE: UpdatePlatformRoleUseCase.cs
//
// PURPOSE:
// Renames a custom role.
//
// RESPONSIBILITIES:
// - Require platform.roles.edit
// - Refuse a system role and a name another role uses
// - Audit the old and new name
//
// IMPORTANT NOTES:
// - System role names come from resources and never change
// =========================================================
public sealed class UpdatePlatformRoleUseCase
{
    private readonly PlatformRoleAdministration _administration;

    private readonly IPlatformRoleRepository _roles;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public UpdatePlatformRoleUseCase(
        PlatformRoleAdministration administration,
        IPlatformRoleRepository roles,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<UpdatePlatformRoleResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        UpdatePlatformRoleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformRoleTarget target = await _administration.LoadRoleAsync(
            context,
            PlatformPermissions.RolesEdit,
            request.PlatformRoleId,
            requireStepUp: false,
            cancellationToken);

        if (target.Role is not PlatformRole role)
        {
            return UpdatePlatformRoleResponse.Failure(target.Error!);
        }

        string before = role.Name;

        try
        {
            role.Rename(request.Name, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return UpdatePlatformRoleResponse.Failure(exception.Error);
        }

        if (string.Equals(before, role.Name, StringComparison.Ordinal))
        {
            return UpdatePlatformRoleResponse.Success();
        }

        if (await _roles.NameExistsAsync(role.Name, role.Id, cancellationToken))
        {
            return UpdatePlatformRoleResponse.Failure(PlatformRoleErrors.NameAlreadyUsed);
        }

        _audit.Record(
            context.PlatformUserId,
            "platform.roles.renamed",
            PlatformPermissions.RolesEdit,
            nameof(PlatformRole),
            role.Id,
            before: new { name = before },
            after: new { name = role.Name });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return UpdatePlatformRoleResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return UpdatePlatformRoleResponse.Success();
    }
}
