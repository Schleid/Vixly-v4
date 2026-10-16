using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.UseCases.Billing.UpdatePlatformCompanyProfile;

// =========================================================
// FILE: UpdatePlatformCompanyProfileUseCase.cs
//
// PURPOSE:
// Sets Vixly's company details printed on invoices (D-B12).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class UpdatePlatformCompanyProfileUseCase
{
    private readonly PlatformBillingAdministration _billing;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public UpdatePlatformCompanyProfileUseCase(
        PlatformBillingAdministration billing,
        IPlatformUnitOfWork unitOfWork)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<UpdatePlatformCompanyProfileResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        UpdatePlatformCompanyProfileRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _billing.AuthorizeAsync(context, PlatformPermissions.BillingConfigure, requireStepUp: true, cancellationToken) is { } refused)
        {
            return UpdatePlatformCompanyProfileResponse.Failure(refused);
        }

        PlatformCompanyProfile? profile = await _billing.Store.GetProfileAsync(cancellationToken);

        try
        {
            PlatformCompanyDetails company = PlatformCompanyDetails.Create(
                request.LegalName,
                request.TradingName,
                EmailAddress.Create(request.Email),
                PhoneNumber.Create(request.Phone),
                PlatformPostalAddress.Create(request.Line1, request.Line2, request.City, request.StateProvince, request.PostalCode, request.CountryCode));

            if (profile is null)
            {
                profile = PlatformCompanyProfile.Create(company, request.TaxId, request.Website, context.PlatformUserId, _billing.UtcNow);
                _billing.Store.AddProfile(profile);
            }
            else
            {
                profile.Update(company, request.TaxId, request.Website, context.PlatformUserId, _billing.UtcNow);
            }

            profile.SetSupportContact(request.SupportEmail, request.SupportPhone, _billing.UtcNow);
        }
        catch (DomainException exception)
        {
            return UpdatePlatformCompanyProfileResponse.Failure(exception.Error);
        }

        _billing.Audit(context, "platform.billing.company_profile_updated", PlatformPermissions.BillingConfigure, nameof(PlatformCompanyProfile), profile.Id,
            new { legalName = profile.Company.LegalName, country = profile.Company.Address.CountryCode, taxId = profile.TaxId });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? UpdatePlatformCompanyProfileResponse.Success()
            : UpdatePlatformCompanyProfileResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
