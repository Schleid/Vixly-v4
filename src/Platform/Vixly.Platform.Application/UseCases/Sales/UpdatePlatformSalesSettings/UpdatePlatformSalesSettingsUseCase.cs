using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases;
using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Sales.UpdatePlatformSalesSettings;

// =========================================================
// FILE: UpdatePlatformSalesSettingsUseCase.cs
//
// PURPOSE:
// Changes the Sales settings: limits, offer validity, discount-ending notice, codes at registration (D-S22, D-S25).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs step-up: these limits decide what sales may give away
// - Every change is audited with the new values
// =========================================================
public sealed class UpdatePlatformSalesSettingsUseCase
{
    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public UpdatePlatformSalesSettingsUseCase(
        PlatformSalesAdministration sales,
        IPlatformUnitOfWork unitOfWork)
    {
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<UpdatePlatformSalesSettingsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        UpdatePlatformSalesSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _sales.AuthorizeAsync(context, PlatformPermissions.SalesConfigure, requireStepUp: true, cancellationToken);

        if (refused is not null)
        {
            return UpdatePlatformSalesSettingsResponse.Failure(refused);
        }

        PlatformSalesSettings? settings = await _sales.SettingsStore.GetAsync(cancellationToken);

        if (settings is null)
        {
            settings = PlatformSalesSettings.Empty(_sales.UtcNow);
            _sales.SettingsStore.Add(settings);
        }

        try
        {
            settings.Update(
                request.MaxPercentWithoutApproval,
                request.FixedLimits ?? new Dictionary<string, decimal>(),
                request.NoEndWithoutApproval,
                request.MaxOfferValidityDays,
                request.DiscountEndNoticeDays,
                request.PromotionCodesAtRegistration,
                context.PlatformUserId,
                _sales.UtcNow);
        }
        catch (DomainException exception)
        {
            return UpdatePlatformSalesSettingsResponse.Failure(exception.Error);
        }

        _sales.Audit(
            context,
            "platform.sales_settings.updated",
            PlatformPermissions.SalesConfigure,
            nameof(PlatformSalesSettings),
            settings.Id,
            after: new
            {
                maxPercent = settings.MaxPercentWithoutApproval,
                fixedLimits = settings.FixedLimits.Select(limit => limit.CurrencyCode + " " + limit.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray(),
                noEnd = settings.NoEndWithoutApproval,
                offerDays = settings.MaxOfferValidityDays,
                noticeDays = settings.DiscountEndNoticeDays,
                codesAtRegistration = settings.PromotionCodesAtRegistration
            });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? UpdatePlatformSalesSettingsResponse.Success()
            : UpdatePlatformSalesSettingsResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
