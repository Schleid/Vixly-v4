using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Domain.Billing;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Billing.ListPlatformTaxRates;

// =========================================================
// FILE: ListPlatformTaxRatesUseCase.cs
//
// PURPOSE:
// Lists tax rates by country and region, newest first (D-B1).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ListPlatformTaxRatesUseCase
{
    private readonly PlatformBillingAdministration _billing;

    public ListPlatformTaxRatesUseCase(
        PlatformBillingAdministration billing)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
    }

    public async Task<ListPlatformTaxRatesResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListPlatformTaxRatesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _billing.AuthorizeAsync(context, PlatformPermissions.BillingView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return ListPlatformTaxRatesResponse.Failure(refused);
        }

        string? country = string.IsNullOrWhiteSpace(request.CountryCode) ? null : request.CountryCode.Trim().ToUpperInvariant();
        IReadOnlyList<PlatformTaxRate> rates = await _billing.Store.ListTaxRatesAsync(country, cancellationToken);
        DateTimeOffset utcNow = _billing.UtcNow;
        HashSet<Guid> inForce = rates
            .GroupBy(rate => (rate.CountryCode, rate.RegionCode))
            .Select(group => group.Where(rate => rate.ValidFromUtc <= utcNow).OrderByDescending(rate => rate.ValidFromUtc).FirstOrDefault())
            .OfType<PlatformTaxRate>()
            .Select(rate => rate.Id)
            .ToHashSet();

        return ListPlatformTaxRatesResponse.Success(
            rates.OrderBy(rate => rate.CountryCode, StringComparer.Ordinal).ThenBy(rate => rate.RegionCode ?? string.Empty, StringComparer.Ordinal).ThenByDescending(rate => rate.ValidFromUtc)
                .Select(rate => new PlatformTaxRateItem(rate.Id, rate.CountryCode, rate.RegionCode, rate.Rate, rate.Label, rate.ValidFromUtc, inForce.Contains(rate.Id)))
                .ToArray(),
            await _billing.HoldsAsync(context, PlatformPermissions.BillingConfigure, cancellationToken));
    }
}
