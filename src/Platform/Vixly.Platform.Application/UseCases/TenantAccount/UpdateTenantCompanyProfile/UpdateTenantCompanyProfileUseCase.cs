using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Tenants;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.TenantAccount.UpdateTenantCompanyProfile;

// =========================================================
// FILE: UpdateTenantCompanyProfileUseCase.cs
//
// PURPOSE:
// The tenant owner updates the company's profile directly: trading name, email, phone and address (owner, 2026-10-25).
//
// RESPONSIBILITIES:
// - Check who may act first (owner, with a checked second factor), then run the rule
// - Keep the legal name as it is; record the change with before and after
//
// IMPORTANT NOTES:
// - No TechCrafters approval; the legal name still changes only through SubmitTenantCompanyChange
// - Return a response; never throw for an expected refusal
// =========================================================
public sealed class UpdateTenantCompanyProfileUseCase
{
    private readonly PlatformTenantChanges _changes;

    public UpdateTenantCompanyProfileUseCase(PlatformTenantChanges changes)
    {
        _changes = changes ?? throw new ArgumentNullException(nameof(changes));
    }

    public async Task<UpdateTenantCompanyProfileResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        UpdateTenantCompanyProfileRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (PlatformTenantAccess.Require(context, true, PlatformTenantActorRole.Owner) is { } notAllowed)
        {
            return UpdateTenantCompanyProfileResponse.Failure(notAllowed);
        }

        PlatformTenant? tenant = await _changes.Tenants.GetAsync(context.PlatformTenantId, cancellationToken);

        if (tenant is null || tenant.Status is PlatformTenantStatus.Banned or PlatformTenantStatus.Deactivated)
        {
            return UpdateTenantCompanyProfileResponse.Failure(PlatformCommonErrors.NotFound);
        }

        PlatformCompanyDetails before = tenant.Company;

        try
        {
            PlatformCompanyInput input = request.Company;
            PlatformCompanyInput profile = new()
            {
                LegalName = before.LegalName,
                TradingName = input.TradingName,
                Email = input.Email,
                Phone = input.Phone,
                AddressLine1 = input.AddressLine1,
                AddressLine2 = input.AddressLine2,
                City = input.City,
                StateProvince = input.StateProvince,
                PostalCode = input.PostalCode,
                CountryCode = input.CountryCode
            };

            if (!tenant.UpdateCompanyProfile(profile.ToDetails(), _changes.UtcNow))
            {
                return UpdateTenantCompanyProfileResponse.Success();
            }
        }
        catch (DomainException exception)
        {
            return UpdateTenantCompanyProfileResponse.Failure(exception.Error);
        }

        _changes.RecordProfile(tenant,
            new { actor = context.ActorReference, actorName = context.ActorName, role = PlatformTenantAccess.RoleName(context.Role) },
            PlatformTenantChanges.Describe(before),
            PlatformTenantChanges.Describe(tenant.Company));

        return await _changes.SaveAsync(cancellationToken)
            ? UpdateTenantCompanyProfileResponse.Success()
            : UpdateTenantCompanyProfileResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
