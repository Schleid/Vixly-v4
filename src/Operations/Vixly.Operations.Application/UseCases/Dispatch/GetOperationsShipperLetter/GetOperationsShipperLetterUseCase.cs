using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Dispatch;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.GetOperationsShipperLetter;

// =========================================================
// FILE: GetOperationsShipperLetterUseCase.cs
//
// PURPOSE:
// The Shipper's Letter of Instruction of a dispatch as a PDF (Slice 6B step 2, D-6; as V3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.dispatches.manage at the dispatch's origin warehouse (D-8)
// =========================================================
public sealed class GetOperationsShipperLetterUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly OperationsDispatchPapers _papers;

    private readonly IOperationsBrandRepository _brands;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsLabelRenderer _renderer;

    private readonly TimeProvider _timeProvider;

    public GetOperationsShipperLetterUseCase(
        OperationsBundleDesk desk,
        IOperationsDispatchRepository dispatches,
        OperationsDispatchPapers papers,
        IOperationsBrandRepository brands,
        IOperationsSettingsRepository settings,
        IOperationsLabelRenderer renderer,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _papers = papers ?? throw new ArgumentNullException(nameof(papers));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsShipperLetterResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsShipperLetterRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.DispatchesManage }, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsShipperLetterResponse.Failure(refused);
        }

        if (await _dispatches.GetDispatchAsync(request.DispatchId, cancellationToken) is not { } dispatch || warehouses.All(item => item.Id != dispatch.WarehouseId))
        {
            return GetOperationsShipperLetterResponse.Failure(OperationsDispatchErrors.NotFound);
        }

        if (dispatch.Status is OperationsDispatchStatus.Cancelled or OperationsDispatchStatus.Planned)
        {
            return GetOperationsShipperLetterResponse.Failure(OperationsDispatchErrors.NotFound);
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        Dictionary<Guid, OperationsCarrier> carriers = (await _dispatches.ListCarriersAsync(cancellationToken)).ToDictionary(carrier => carrier.Id);
        OperationsShipperLetter letter = await _papers.LetterAsync(dispatch, (await _brands.FindAsync(cancellationToken))?.DisplayName ?? string.Empty, carriers, settings,
            OperationsTimeZones.Today(_timeProvider.GetUtcNow(), settings.TimeZone), cancellationToken);
        return GetOperationsShipperLetterResponse.Success(_renderer.RenderShipperLetter(letter), dispatch.Number + "-SLI.pdf");
    }
}
