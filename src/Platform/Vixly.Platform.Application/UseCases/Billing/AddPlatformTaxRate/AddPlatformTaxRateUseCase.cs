using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Domain.Billing;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Billing.AddPlatformTaxRate;

// =========================================================
// FILE: AddPlatformTaxRateUseCase.cs
//
// PURPOSE:
// Adds a tax rate for a country or region from a date; earlier rates stay for issued invoices (D-B1).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class AddPlatformTaxRateUseCase
{
    private readonly PlatformBillingAdministration _billing;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public AddPlatformTaxRateUseCase(
        PlatformBillingAdministration billing,
        IPlatformUnitOfWork unitOfWork)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<AddPlatformTaxRateResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        AddPlatformTaxRateRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _billing.AuthorizeAsync(context, PlatformPermissions.BillingConfigure, requireStepUp: true, cancellationToken) is { } refused)
        {
            return AddPlatformTaxRateResponse.Failure(refused);
        }

        PlatformTaxRate rate;

        try
        {
            rate = PlatformTaxRate.Create(request.CountryCode, request.RegionCode, request.Rate, request.Label, request.ValidFromUtc, context.PlatformUserId, _billing.UtcNow);
        }
        catch (DomainException exception)
        {
            return AddPlatformTaxRateResponse.Failure(exception.Error);
        }

        _billing.Store.AddTaxRate(rate);
        _billing.Audit(context, "platform.billing.tax_rate_added", PlatformPermissions.BillingConfigure, nameof(PlatformTaxRate), rate.Id,
            new { country = rate.CountryCode, region = rate.RegionCode, rate = rate.Rate, label = rate.Label, from = rate.ValidFromUtc });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? AddPlatformTaxRateResponse.Success()
            : AddPlatformTaxRateResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
