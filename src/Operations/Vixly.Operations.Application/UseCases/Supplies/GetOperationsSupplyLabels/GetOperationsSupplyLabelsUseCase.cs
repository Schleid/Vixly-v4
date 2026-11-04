using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Supplies;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.GetOperationsSupplyLabels;

// =========================================================
// FILE: GetOperationsSupplyLabelsUseCase.cs
//
// PURPOSE:
// A sheet of shelf labels for a supply: its name, its item code as a barcode and its price today (Slice 5E, S-3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.charges.configure: the people who set prices manage supplies (Slice 5E, S-10)
// =========================================================
public sealed class GetOperationsSupplyLabelsUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSupplyRepository _supplies;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsLabelRenderer _renderer;

    private readonly TimeProvider _timeProvider;

    public GetOperationsSupplyLabelsUseCase(
        OperationsAccess access,
        IOperationsSupplyRepository supplies,
        IOperationsSettingsRepository settings,
        IOperationsLabelRenderer renderer,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _supplies = supplies ?? throw new ArgumentNullException(nameof(supplies));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsSupplyLabelsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsSupplyLabelsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? _, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.ChargesConfigure, false, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsSupplyLabelsResponse.Failure(refused);
        }

        if (await _supplies.GetAsync(request.SupplyId, cancellationToken) is not { } supply)
        {
            return GetOperationsSupplyLabelsResponse.Failure(OperationsSupplyErrors.NotFound);
        }

        // The item code is scanned like a barcode at the counter, so an item without one gets its code printed.
        decimal? price = supply.UnitPrice(null, 1, _timeProvider.GetUtcNow());
        string currency = (await _settings.FindAsync(cancellationToken))?.CurrencyCode ?? OperationsTenantSettings.DefaultCurrency;
        OperationsSupplyLabel label = new(supply.NameEn, supply.Code, price, currency);
        return GetOperationsSupplyLabelsResponse.Success(_renderer.RenderSupplyLabels(label, Math.Clamp(request.Count, 1, 30)), "supply-" + supply.Code.ToLowerInvariant() + ".pdf");
    }
}
