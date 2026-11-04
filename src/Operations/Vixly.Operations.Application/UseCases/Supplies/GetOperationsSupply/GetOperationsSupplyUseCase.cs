using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.GetOperationsSupply;

// =========================================================
// FILE: GetOperationsSupplyUseCase.cs
//
// PURPOSE:
// One shipping supply with its barcodes and prices (Slice 5E, S-3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.charges.configure: the people who set prices manage supplies (Slice 5E, S-10)
// =========================================================
public sealed class GetOperationsSupplyUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSupplyRepository _supplies;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public GetOperationsSupplyUseCase(
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

    public async Task<GetOperationsSupplyResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsSupplyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? _, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.ChargesConfigure, false, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsSupplyResponse.Failure(refused);
        }

        if (await _supplies.GetAsync(request.SupplyId, cancellationToken) is not { } supply)
        {
            return GetOperationsSupplyResponse.Failure(OperationsSupplyErrors.NotFound);
        }

        return GetOperationsSupplyResponse.Success(
            supply,
            await _locations.ListWarehousesAsync(cancellationToken),
            (await _settings.FindAsync(cancellationToken))?.CurrencyCode ?? OperationsTenantSettings.DefaultCurrency,
            _timeProvider.GetUtcNow());
    }
}
