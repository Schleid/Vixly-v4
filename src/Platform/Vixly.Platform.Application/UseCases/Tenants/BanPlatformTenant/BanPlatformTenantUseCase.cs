using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Tenants.BanPlatformTenant;

// =========================================================
// FILE: BanPlatformTenantUseCase.cs
//
// PURPOSE:
// Bans a tenant for good (D-T1).
//
// RESPONSIBILITIES:
// - Check platform.tenants.ban and step-up
// - Apply the move in the domain (D-T1)
// - Record the history event, audit entry and outbox message
//   in one commit (D-2, D-T17)
//
// IMPORTANT NOTES:
// - Needs SuperUser authority as well as the permission
// - Terminal: no move leaves Banned
// =========================================================
public sealed class BanPlatformTenantUseCase
{
    private readonly PlatformTenantAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public BanPlatformTenantUseCase(
        PlatformTenantAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<BanPlatformTenantResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        BanPlatformTenantRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformTenantTarget target = await _administration.LoadTenantAsync(
            context,
            PlatformPermissions.TenantsBan,
            request.PlatformTenantId,
            requireStepUp: true,
            cancellationToken);

        if (target.Tenant is not PlatformTenant tenant)
        {
            return BanPlatformTenantResponse.Failure(target.Error!);
        }

        if (context.AuthorityLevel != PlatformUserAuthorityLevel.SuperUser)
        {
            return BanPlatformTenantResponse.Failure(PlatformTenantAdminErrors.BanNeedsSuperUser);
        }

        PlatformTenantLifecycleEvent change;

        try
        {
            change = tenant.Ban(request.Reason, context.PlatformUserId, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return BanPlatformTenantResponse.Failure(exception.Error);
        }

        _administration.RecordMove(context, tenant, change, PlatformPermissions.TenantsBan, "platform.tenants.banned");

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return BanPlatformTenantResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return BanPlatformTenantResponse.Success();
    }
}
