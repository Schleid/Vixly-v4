using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Domain.Billing;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Billing.UpdatePlatformBillingSettings;

// =========================================================
// FILE: UpdatePlatformBillingSettingsUseCase.cs
//
// PURPOSE:
// Changes payment terms, number prefixes, paper size, the email switch and the invoice texts (D-B4).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class UpdatePlatformBillingSettingsUseCase
{
    private readonly PlatformBillingAdministration _billing;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public UpdatePlatformBillingSettingsUseCase(
        PlatformBillingAdministration billing,
        IPlatformUnitOfWork unitOfWork)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<UpdatePlatformBillingSettingsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        UpdatePlatformBillingSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _billing.AuthorizeAsync(context, PlatformPermissions.BillingConfigure, requireStepUp: true, cancellationToken) is { } refused)
        {
            return UpdatePlatformBillingSettingsResponse.Failure(refused);
        }

        PlatformBillingSettings? settings = await _billing.Store.GetSettingsAsync(cancellationToken);
        bool created = settings is null;
        settings ??= PlatformBillingSettings.Create(_billing.UtcNow);

        try
        {
            Dictionary<LanguageCode, (string?, string?)> texts = new();

            foreach ((string language, PlatformBillingTextItem text) in request.Texts ?? new Dictionary<string, PlatformBillingTextItem>())
            {
                texts[PlatformLanguageInput.Read(language)] = (text.PaymentInstructions, text.Footer);
            }

            settings.Update(request.PaymentTermsDays, request.InvoicePrefix, request.CreditNotePrefix, request.ReceiptPrefix, request.PaperSize, request.EmailDocuments, texts, context.PlatformUserId, _billing.UtcNow);
        }
        catch (DomainException exception)
        {
            return UpdatePlatformBillingSettingsResponse.Failure(exception.Error);
        }

        if (created)
        {
            _billing.Store.AddSettings(settings);
        }

        _billing.Audit(context, "platform.billing.settings_updated", PlatformPermissions.BillingConfigure, nameof(PlatformBillingSettings), settings.Id,
            new { terms = settings.PaymentTermsDays, invoicePrefix = settings.InvoicePrefix, paper = settings.PaperSize.ToString(), email = settings.EmailDocuments });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? UpdatePlatformBillingSettingsResponse.Success()
            : UpdatePlatformBillingSettingsResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
