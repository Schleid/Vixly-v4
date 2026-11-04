using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.ListOperationsSupplies;

// =========================================================
// FILE: ListOperationsSuppliesUseCase.cs
//
// PURPOSE:
// The company's shipping supplies with their current price (Slice 5E, S-3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.charges.configure: the people who set prices manage supplies (Slice 5E, S-10)
// =========================================================
public sealed class ListOperationsSuppliesUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSupplyRepository _supplies;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public ListOperationsSuppliesUseCase(
        OperationsAccess access,
        IOperationsSupplyRepository supplies,
        IOperationsLocationRepository locations,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _supplies = supplies ?? throw new ArgumentNullException(nameof(supplies));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ListOperationsSuppliesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsSuppliesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? _, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.ChargesConfigure, false, cancellationToken);

        if (refused is not null)
        {
            return ListOperationsSuppliesResponse.Failure(refused);
        }

        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);
        return ListOperationsSuppliesResponse.Success(
            await _supplies.ListAsync(cancellationToken),
            await _locations.ListWarehousesAsync(cancellationToken),
            settings?.CurrencyCode ?? OperationsTenantSettings.DefaultCurrency,
            _timeProvider.GetUtcNow(),
            settings?.CountSupplyStock ?? true,
            settings?.SupplyStockOut ?? OperationsSupplyStockOut.AllowWithWarning,
            settings);
    }
}
