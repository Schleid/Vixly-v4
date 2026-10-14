using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Roles.CreatePlatformRole;

// =========================================================
// FILE: CreatePlatformRoleUseCase.cs
//
// PURPOSE:
// Creates a custom role with no permissions yet.
//
// RESPONSIBILITIES:
// - Require platform.roles.create
// - Refuse a name another role already uses
// - Audit the creation
//
// IMPORTANT NOTES:
// - Permissions are added afterwards with UpdateRolePermissions,
//   which has its own permission and step-up
// - A custom role's name is not translated (L-5)
// =========================================================
public sealed class CreatePlatformRoleUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformRoleRepository _roles;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public CreatePlatformRoleUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformRoleRepository roles,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CreatePlatformRoleResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        CreatePlatformRoleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.RolesCreate, cancellationToken);

        PlatformRole role;

        try
        {
            role = PlatformRole.CreateCustom(request.Name, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return CreatePlatformRoleResponse.Failure(exception.Error);
        }

        if (await _roles.NameExistsAsync(role.Name, null, cancellationToken))
        {
            return CreatePlatformRoleResponse.Failure(PlatformRoleErrors.NameAlreadyUsed);
        }

        _roles.Add(role);
        _audit.Record(
            context.PlatformUserId,
            "platform.roles.created",
            PlatformPermissions.RolesCreate,
            nameof(PlatformRole),
            role.Id,
            after: new { name = role.Name });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return CreatePlatformRoleResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return CreatePlatformRoleResponse.Success(role.Id);
    }
}
