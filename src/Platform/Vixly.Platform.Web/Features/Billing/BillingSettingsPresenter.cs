using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Billing.GetPlatformBillingSettings;
using Vixly.Platform.Application.UseCases.Billing.SetPlatformCompanyLogo;
using Vixly.Platform.Application.UseCases.Billing.UpdatePlatformBillingSettings;
using Vixly.Platform.Application.UseCases.Billing.UpdatePlatformCompanyProfile;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Billing;

// =========================================================
// FILE: BillingSettingsPresenter.cs
//
// PURPOSE:
// Behaviour of the Billing settings page (D-B2, D-B4, D-B10, D-B12).
//
// RESPONSIBILITIES:
// - Load the settings and the company profile
// - Save the settings, the profile or a new logo
//
// IMPORTANT NOTES:
// - Every change asks for step-up; the use case decides
// =========================================================
public sealed class BillingSettingsPresenter : PlatformPresenter<BillingSettingsState>
{
    private readonly GetPlatformBillingSettingsUseCase _get;

    private readonly UpdatePlatformBillingSettingsUseCase _update;

    private readonly UpdatePlatformCompanyProfileUseCase _profile;

    private readonly SetPlatformCompanyLogoUseCase _logo;

    public BillingSettingsPresenter(
        GetPlatformBillingSettingsUseCase get,
        UpdatePlatformBillingSettingsUseCase update,
        UpdatePlatformCompanyProfileUseCase profile,
        SetPlatformCompanyLogoUseCase logo,
        PlatformUseCaseScope scope,
        ILogger<BillingSettingsPresenter> logger)
        : base(scope, BillingSettingsState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _update = update ?? throw new ArgumentNullException(nameof(update));
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _logo = logo ?? throw new ArgumentNullException(nameof(logo));
    }

    public async Task LoadAsync()
    {
        GetPlatformBillingSettingsResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformBillingSettingsRequest(), cancellationToken);
            return response.Error;
        });

        SetState(error is null && response is not null
            ? State with { Settings = response.Settings, CanEdit = response.CanEdit, Loaded = true, Version = State.Version + 1 }
            : State with { Loaded = true, Error = error });
    }

    public void CancelStepUp()
    {
        SetState(State with { StepUpPending = false });
    }

    public Task SaveSettingsAsync(BillingSettingsModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return SaveAsync("ui.billing_settings.saved", async (context, cancellationToken) => (await _update.ExecuteAsync(
            context,
            new UpdatePlatformBillingSettingsRequest
            {
                PaymentTermsDays = model.PaymentTermsDays,
                InvoicePrefix = model.InvoicePrefix,
                CreditNotePrefix = model.CreditNotePrefix,
                ReceiptPrefix = model.ReceiptPrefix,
                PaperSize = model.PaperSize,
                EmailDocuments = model.EmailDocuments,
                Texts = model.TextItems()
            },
            cancellationToken)).Error);
    }

    public Task SaveProfileAsync(CompanyProfileModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return SaveAsync("ui.billing_settings.profile_saved", async (context, cancellationToken) => (await _profile.ExecuteAsync(
            context,
            new UpdatePlatformCompanyProfileRequest
            {
                LegalName = model.LegalName,
                TradingName = model.TradingName,
                Email = model.Email,
                Phone = model.Phone,
                Line1 = model.Line1,
                Line2 = model.Line2,
                City = model.City,
                StateProvince = model.StateProvince,
                PostalCode = model.PostalCode,
                CountryCode = model.CountryCode,
                TaxId = model.TaxId,
                Website = model.Website,
                SupportEmail = model.SupportEmail,
                SupportPhone = model.SupportPhone
            },
            cancellationToken)).Error);
    }

    public Task SaveLogoAsync(byte[] content, string contentType)
    {
        return SaveAsync("ui.billing_settings.logo_saved", async (context, cancellationToken) => (await _logo.ExecuteAsync(
            context, new SetPlatformCompanyLogoRequest { Content = content, ContentType = contentType }, cancellationToken)).Error);
    }

    public void Fail(Error error)
    {
        SetState(State with { DoneKey = null, Error = error });
    }

    private async Task SaveAsync(string doneKey, Func<PlatformAuthorizationContext, CancellationToken, Task<Error?>> work)
    {
        Error? error = await RunUseCaseAsync(work);

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { StepUpPending = true, Error = null });
            return;
        }

        SetState(State with { StepUpPending = false, DoneKey = error is null ? doneKey : null, Error = error });

        if (error is null)
        {
            await LoadAsync();
        }
    }
}
