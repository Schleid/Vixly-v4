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

namespace Vixly.Platform.Application.UseCases.Sales.GetPlatformSalesSettings;

// =========================================================
// FILE: GetPlatformSalesSettingsUseCase.cs
//
// PURPOSE:
// Reads the Sales settings (D-S22).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Salespeople can read the limits they work within; only the sales manager changes them
// =========================================================
public sealed class GetPlatformSalesSettingsUseCase
{
    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformSubscriptionQueries _queries;

    public GetPlatformSalesSettingsUseCase(
        PlatformSalesAdministration sales,
        IPlatformSubscriptionQueries queries)
    {
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    public async Task<GetPlatformSalesSettingsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformSalesSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _sales.AuthorizeAsync(context, PlatformPermissions.DiscountsGrant, requireStepUp: false, cancellationToken);

        if (refused is not null)
        {
            return GetPlatformSalesSettingsResponse.Failure(refused);
        }

        PlatformSalesSettings? settings = await _sales.SettingsStore.GetAsync(cancellationToken);
        string? by = null;

        if (settings?.UpdatedByUserId is Guid editor)
        {
            (await _queries.StaffNamesAsync(new[] { editor }, cancellationToken)).TryGetValue(editor, out by);
        }

        return GetPlatformSalesSettingsResponse.Success(
            PlatformSalesSettingsItem.From(settings, by),
            await _sales.HoldsAsync(context, PlatformPermissions.SalesConfigure, cancellationToken));
    }
}
