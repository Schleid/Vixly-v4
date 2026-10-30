using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Application.UseCases.Receiving.ListOperationsIncoming;

// =========================================================
// FILE: ListOperationsIncomingUseCase.cs
//
// PURPOSE:
// The incoming packages list: every pre-alert of the warehouses the staff member may see, filtered (Slice 4D, A-7).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - An empty warehouse choice shows every warehouse the person may see
// - The export holds at most MaxExport rows
// =========================================================
public sealed class ListOperationsIncomingUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public ListOperationsIncomingUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsReceivingRepository receiving,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ListOperationsIncomingResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsIncomingRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return ListOperationsIncomingResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        IReadOnlyList<OperationsWarehouse> warehouses = await _scope.WarehousesAsync(access, OperationsPermissions.ParcelsView, false, cancellationToken);

        if (warehouses.Count == 0)
        {
            return ListOperationsIncomingResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        // Scoped by place, like the parcels (design section 8 of Slice 3).
        List<Guid> shown = warehouses.Where(warehouse => request.WarehouseId is null || warehouse.Id == request.WarehouseId).Select(warehouse => warehouse.Id).ToList();
        DateOnly today = OperationsTimeZones.Today(_timeProvider.GetUtcNow(), (await _settings.FindAsync(cancellationToken))?.TimeZone);
        OperationsIncomingFilter filter = new(shown, request.View, today, request.From, request.To, Trimmed(request.Carrier, 40), Trimmed(request.Search, 60));

        (IReadOnlyList<OperationsIncomingRow> rows, int total) = await _receiving.ListIncomingAsync(filter, request.Export ? 1 : Math.Max(request.Page, 1),
            request.Export ? MaxExport : PageSize, cancellationToken);

        return ListOperationsIncomingResponse.Success(rows, total, await _receiving.CountIncomingAsync(filter, cancellationToken), warehouses, today);
    }

    public const int PageSize = 50;

    public const int MaxExport = 5000;

    private static string? Trimmed(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().Length > max ? value.Trim()[..max] : value.Trim();
}
