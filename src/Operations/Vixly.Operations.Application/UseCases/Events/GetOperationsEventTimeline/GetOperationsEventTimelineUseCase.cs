using Vixly.Operations.Application.Events;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Events;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Events.GetOperationsEventTimeline;

// =========================================================
// FILE: GetOperationsEventTimelineUseCase.cs
//
// PURPOSE:
// One subject's history in the events workspace (Slice 6C): its events newest first, its parcels, and what can be added.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.events.view or ops.events.add, at the warehouse the subject leaves from or goes to (E-6)
// =========================================================
public sealed class GetOperationsEventTimelineUseCase
{
    private readonly OperationsEventDesk _desk;

    private readonly IOperationsEventRepository _events;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public GetOperationsEventTimelineUseCase(
        OperationsEventDesk desk,
        IOperationsEventRepository events,
        IOperationsTrackingRepository tracking,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsEventTimelineResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsEventTimelineRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, OperationsPermissions.EventsView, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsEventTimelineResponse.Failure(refused);
        }

        if (await _desk.SubjectAsync(request.Kind, request.Id, cancellationToken) is not { } subject || !subject.Covers(warehouses.Select(item => item.Id)))
        {
            return GetOperationsEventTimelineResponse.Failure(OperationsEventErrors.NotFound);
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        const int PageSize = 50;
        IEnumerable<OperationsTrackingLine> lines = OperationsTrackingViews.ForStaff(await _tracking.ListForSubjectAsync(subject.Kind, subject.Id, cancellationToken)).Reverse();
        lines = request.Filter switch
        {
            "customer" => lines.Where(line => OperationsTrackingCodes.Find(line.Code)?.Visibility == OperationsTrackingVisibility.Customer && !line.Voided),
            "manual" => lines.Where(line => OperationsTrackingCodes.ManualCodes.Contains(line.Code, StringComparer.Ordinal)),
            "voided" => lines.Where(line => line.Voided || line.VoidsEventId is not null),
            _ => lines
        };
        List<OperationsTrackingLine> all = lines.ToList();
        int pages = Math.Max(1, (all.Count + PageSize - 1) / PageSize);
        int page = Math.Clamp(request.Page, 1, pages);
        IReadOnlyList<OperationsParcel> parcels = subject.Kind == OperationsTrackingSubject.Parcel ? Array.Empty<OperationsParcel>()
            : await _events.ListSubjectParcelsAsync(subject.Kind, subject.Id, 200, cancellationToken);
        bool canAdd = warehouses.Any(warehouse => (warehouse.Id == subject.OriginWarehouseId || warehouse.Id == subject.DestinationWarehouseId || warehouse.Id == subject.HereWarehouseId)
            && access!.CoversWarehouse(OperationsPermissions.EventsAdd, warehouse.Id, warehouse.BranchId));
        return GetOperationsEventTimelineResponse.Success(subject, all.Skip((page - 1) * PageSize).Take(PageSize).ToList(), pages, parcels, canAdd,
            await _desk.CausesAsync(OperationsEventKind.Delay, settings, cancellationToken), await _desk.CausesAsync(OperationsEventKind.Exception, settings, cancellationToken), settings.TimeZone);
    }
}
