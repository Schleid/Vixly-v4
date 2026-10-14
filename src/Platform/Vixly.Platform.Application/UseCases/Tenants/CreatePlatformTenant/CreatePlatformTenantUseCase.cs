using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.UseCases.Tenants.CreatePlatformTenant;

// =========================================================
// FILE: CreatePlatformTenantUseCase.cs
//
// PURPOSE:
// Creates a tenant from the console for sales-led onboarding
// (D-T8).
//
// RESPONSIBILITIES:
// - Check platform.tenants.create
// - Check the slug (format, reserved, taken) and claim it for good
// - Generate a unique tenant code (D-T13)
// - Create the tenant in Draft with its owner contact, history
//   event and audit entry in one commit
//
// IMPORTANT NOTES:
// - A racing claim of the same slug fails the save: reported as
//   slug_taken
// - V3's unused RegisterTenant is not ported
// =========================================================
public sealed class CreatePlatformTenantUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly PlatformTenantAdministration _administration;

    private readonly PlatformPortalSlugs _slugs;

    private readonly IPlatformPortalSlugRepository _slugClaims;

    private readonly IPlatformTenantRepository _tenants;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public CreatePlatformTenantUseCase(
        IPlatformAuthorizationGate gate,
        PlatformTenantAdministration administration,
        PlatformPortalSlugs slugs,
        IPlatformPortalSlugRepository slugClaims,
        IPlatformTenantRepository tenants,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _slugs = slugs ?? throw new ArgumentNullException(nameof(slugs));
        _slugClaims = slugClaims ?? throw new ArgumentNullException(nameof(slugClaims));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CreatePlatformTenantResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        CreatePlatformTenantRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.TenantsCreate, cancellationToken);

        DateTimeOffset utcNow = _administration.UtcNow;
        PlatformSlugCheck slug = await _slugs.CheckAsync(request.Slug, utcNow, cancellationToken);

        if (slug.Error is not null)
        {
            return CreatePlatformTenantResponse.Failure(slug.Error);
        }

        string? code = await _administration.NewTenantCodeAsync(cancellationToken);

        if (code is null)
        {
            return CreatePlatformTenantResponse.Failure(PlatformTenantAdminErrors.CodeUnavailable);
        }

        PlatformTenantCreation creation;

        try
        {
            PlatformTenantContactDetails owner = PlatformTenantContactDetails.Create(
                PlatformTenantContactRole.Owner,
                request.OwnerName,
                EmailAddress.Create(request.OwnerEmail),
                string.IsNullOrWhiteSpace(request.OwnerPhone) ? null : PhoneNumber.Create(request.OwnerPhone));

            creation = PlatformTenant.CreateByStaff(
                code,
                slug.Slug!,
                request.Company.ToDetails(),
                owner,
                LanguageCode.Create(request.Language),
                request.TimeZone,
                context.PlatformUserId,
                utcNow);

            if (slug.LapsedClaim is { } lapsed)
            {
                lapsed.ReclaimForTenant(creation.Tenant.Id, utcNow);
            }
            else
            {
                _slugClaims.Add(PlatformPortalSlug.ClaimForTenant(slug.Slug!, creation.Tenant.Id, utcNow));
            }
        }
        catch (DomainException exception)
        {
            return CreatePlatformTenantResponse.Failure(exception.Error);
        }

        _tenants.Add(creation.Tenant);
        _tenants.AddEvents(creation.Events);
        _audit.Record(
            context.PlatformUserId,
            "platform.tenants.created",
            PlatformPermissions.TenantsCreate,
            nameof(PlatformTenant),
            creation.Tenant.Id,
            after: new { code, slug = slug.Slug, origin = "staff" });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return CreatePlatformTenantResponse.Failure(PlatformTenantErrors.SlugTaken);
        }

        return CreatePlatformTenantResponse.Success(creation.Tenant.Id, code);
    }
}
