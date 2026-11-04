using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Cards;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.GetOperationsCardSettings;

// =========================================================
// FILE: GetOperationsCardSettingsUseCase.cs
//
// PURPOSE:
// The company's Stripe account and card readers as Settings → Payments shows them (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Never the keys: only the secret key's last 4 characters
// - Needs ops.settings.view
// =========================================================
public sealed class GetOperationsCardSettingsUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsCardRepository _cards;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsLocationRepository _locations;

    public GetOperationsCardSettingsUseCase(
        OperationsAccess access,
        IOperationsCardRepository cards,
        IOperationsSettingsRepository settings,
        IOperationsLocationRepository locations)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _cards = cards ?? throw new ArgumentNullException(nameof(cards));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
    }

    public async Task<GetOperationsCardSettingsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsCardSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsView, false, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsCardSettingsResponse.Failure(refused);
        }

        OperationsCardProvider? provider = await _cards.GetProviderAsync(cancellationToken);
        IReadOnlyList<OperationsWarehouse> all = await _locations.ListWarehousesAsync(cancellationToken);
        Dictionary<Guid, string> names = all.ToDictionary(warehouse => warehouse.Id, warehouse => warehouse.Name);
        IReadOnlyList<OperationsCardReaderRow> readers = (await _cards.ListReadersAsync(cancellationToken))
            .Select(reader => new OperationsCardReaderRow(reader.Id, reader.Label, reader.StripeReaderId, reader.WarehouseId, names.GetValueOrDefault(reader.WarehouseId, string.Empty)))
            .ToList();
        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);
        return GetOperationsCardSettingsResponse.Success(provider is not null, provider?.KeyHint, provider?.Live == true, provider?.AccountName,
            settings?.Accepts(OperationsPaymentMethod.Card) == true, access!.HasTenantWide(OperationsPermissions.SettingsEdit), readers,
            all.Where(warehouse => warehouse.Status == OperationsLocationStatus.Active).OrderBy(warehouse => warehouse.Name, StringComparer.CurrentCulture)
                .Select(warehouse => new Vixly.Operations.Application.Shipments.OperationsChoice(warehouse.Id, warehouse.Name, warehouse.Code)).ToList());
    }
}
