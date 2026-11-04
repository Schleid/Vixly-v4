using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;

namespace Vixly.Operations.Application.UseCases.Staff.GetOperationsMenu;

// =========================================================
// FILE: GetOperationsMenuUseCase.cs
//
// PURPOSE:
// What the signed-in person may do, so the staff site shows only that (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Permissions held in any scope count: the page itself then shows only the branches and warehouses of that scope
// =========================================================
public sealed class GetOperationsMenuUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly Vixly.Operations.Application.Ports.IOperationsPlanFeatures _features;

    private readonly Vixly.Operations.Application.Ports.IOperationsDashboardReader _counts;

    private readonly Vixly.Operations.Application.Billing.OperationsBillingDesk _desk;

    public GetOperationsMenuUseCase(OperationsOwnerCheck owner, Vixly.Operations.Application.Ports.IOperationsPlanFeatures features,
        Vixly.Operations.Application.Ports.IOperationsDashboardReader counts,
        Vixly.Operations.Application.Billing.OperationsBillingDesk desk)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _features = features ?? throw new ArgumentNullException(nameof(features));
        _counts = counts ?? throw new ArgumentNullException(nameof(counts));
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
    }

    public async Task<GetOperationsMenuResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsMenuRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return GetOperationsMenuResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        Vixly.Operations.Application.Ports.OperationsPlanFeatures features = await _features.GetAsync(context.RequiredTenantId, cancellationToken);
        // Owner, 2026-11-04: counts beside the menu, only for those who can act on them.
        // Harmonization H-3: both counts in one database command.
        Dictionary<string, int> counts = new(StringComparer.Ordinal);
        Dictionary<string, IReadOnlyCollection<Guid>?> asks = new(StringComparer.Ordinal);

        if (access.Has(OperationsPermissions.BrandEdit) && features.Includes(Vixly.Operations.Application.Plans.OperationsPlanModules.CustomerPortal))
        {
            asks["reviews_new"] = null;
        }

        if (access.Has(OperationsPermissions.PaymentsConfirm))
        {
            IReadOnlyList<Vixly.Operations.Domain.Locations.OperationsWarehouse> warehouses = await _desk.WarehousesAsync(access, OperationsPermissions.PaymentsConfirm, cancellationToken);

            if (warehouses.Count == 0)
            {
                counts["verify"] = 0;
            }
            else
            {
                asks["payments_to_verify"] = warehouses.Select(warehouse => warehouse.Id).ToList();
            }
        }

        if (asks.Count > 0)
        {
            // Dates do not matter to these two counts.
            IReadOnlyDictionary<string, int> found = await _counts.CountManyAsync(asks, DateOnly.MinValue, cancellationToken);

            if (found.TryGetValue("reviews_new", out int reviews))
            {
                counts["site"] = reviews;
            }

            if (found.TryGetValue("payments_to_verify", out int verify))
            {
                counts["verify"] = verify;
            }
        }

        return GetOperationsMenuResponse.Success(OperationsPermissions.All.Where(access.Has).ToHashSet(StringComparer.Ordinal), access.IsOwner, access.IsAdministrator,
            Vixly.Operations.Application.Plans.OperationsPlanModules.HiddenPages(features), !features.Includes(Vixly.Operations.Application.Plans.OperationsPlanModules.CustomerPortal),
            features.ModulesOff).WithCounts(counts);
    }
}
