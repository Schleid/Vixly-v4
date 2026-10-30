using Vixly.Operations.Application.Arrivals;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Delivery;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Pickup;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Delivery;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Pickup;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Delivery.GetOperationsRunSheet;

// =========================================================
// FILE: GetOperationsRunSheetUseCase.cs
//
// PURPOSE:
// A delivery route's run sheet as a PDF (Slice 7 step 3, R-7, R-12).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.delivery.plan at its warehouse, or its own driver
// =========================================================
public sealed class GetOperationsRunSheetUseCase
{
    private readonly OperationsDeliveryDesk _desk;

    private readonly IOperationsBrandRepository _brands;

    private readonly IOperationsLabelRenderer _renderer;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public GetOperationsRunSheetUseCase(
        OperationsDeliveryDesk desk,
        IOperationsBrandRepository brands,
        IOperationsLabelRenderer renderer,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsRunSheetResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsRunSheetRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, OperationsDeliveryRouteView? view, bool planner, Error? refused) = await _desk.RouteForAsync(context, request.RouteId, cancellationToken);

        if (refused is not null || view is null)
        {
            return GetOperationsRunSheetResponse.Failure(refused ?? OperationsDeliveryErrors.NotFound);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        // A partner agent whose partner does not handle money sees no amounts (R-8), on paper too.
        bool money = access!.HandlesMoneyAt(view.Warehouse);
        OperationsRunSheet sheet = new((await _brands.FindAsync(cancellationToken))?.DisplayName ?? string.Empty, view.Route.Number, view.Warehouse.Name, view.Route.ServiceDate,
            view.Route.DriverName, (await _desk.StopRowsAsync(view, settings, cancellationToken)).Select(stop => money ? stop : stop with { Due = 0m }).ToList(),
            settings.DeliveryProofs, settings.CurrencyCode);
        return GetOperationsRunSheetResponse.Success(_renderer.RenderRunSheet(sheet), view.Route.Number);
    }
}
