using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Billing.AddPlatformTaxRate;
using Vixly.Platform.Application.UseCases.Billing.ListPlatformTaxRates;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Billing;

// =========================================================
// FILE: TaxRateListPresenter.cs
//
// PURPOSE:
// Behaviour of the Tax rates page (D-B1).
//
// RESPONSIBILITIES:
// - List the rates, optionally for one country
// - Add a rate valid from a date
//
// IMPORTANT NOTES:
// - A rate is never edited; a new rate with a later start replaces it
// - Adding asks for step-up; the use case decides
// =========================================================
public sealed class TaxRateListPresenter : PlatformPresenter<TaxRateListState>
{
    private readonly ListPlatformTaxRatesUseCase _list;

    private readonly AddPlatformTaxRateUseCase _add;

    public TaxRateListPresenter(ListPlatformTaxRatesUseCase list, AddPlatformTaxRateUseCase add, PlatformUseCaseScope scope, ILogger<TaxRateListPresenter> logger)
        : base(scope, TaxRateListState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
        _add = add ?? throw new ArgumentNullException(nameof(add));
    }

    public async Task LoadAsync(string? countryCode)
    {
        ListPlatformTaxRatesResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _list.ExecuteAsync(context, new ListPlatformTaxRatesRequest { CountryCode = string.IsNullOrEmpty(countryCode) ? null : countryCode }, cancellationToken);
            return response.Error;
        });

        SetState(error is null && response is not null
            ? State with { Rates = response.Rates, CanEdit = response.CanEdit, Loaded = true }
            : State with { Loaded = true, Error = error });
    }

    public void CancelStepUp()
    {
        SetState(State with { StepUpPending = false });
    }

    public async Task<bool> AddAsync(string? countryCode, string? regionCode, decimal rate, string? label, DateTime? validFrom, string? filter)
    {
        DateTimeOffset from = validFrom is DateTime day
            ? new DateTimeOffset(DateTime.SpecifyKind(day.Date, DateTimeKind.Utc))
            : DateTimeOffset.UtcNow;

        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => (await _add.ExecuteAsync(
            context,
            new AddPlatformTaxRateRequest { CountryCode = countryCode, RegionCode = string.IsNullOrWhiteSpace(regionCode) ? null : regionCode.Trim(), Rate = rate, Label = label, ValidFromUtc = from },
            cancellationToken)).Error);

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { StepUpPending = true, Error = null });
            return false;
        }

        SetState(State with { StepUpPending = false, DoneKey = error is null ? "ui.tax_rates.added" : null, Error = error });

        if (error is null)
        {
            await LoadAsync(filter);
        }

        return error is null;
    }
}
