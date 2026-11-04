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

    private readonly Vixly.Operations.Application.Ports.IOperationsSiteRepository _site;

    private readonly Vixly.Operations.Application.Ports.IOperationsPaymentRepository _payments;

    private readonly Vixly.Operations.Application.Billing.OperationsBillingDesk _desk;

    public GetOperationsMenuUseCase(OperationsOwnerCheck owner, Vixly.Operations.Application.Ports.IOperationsPlanFeatures features,
        Vixly.Operations.Application.Ports.IOperationsSiteRepository site, Vixly.Operations.Application.Ports.IOperationsPaymentRepository payments,
        Vixly.Operations.Application.Billing.OperationsBillingDesk desk)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _features = features ?? throw new ArgumentNullException(nameof(features));
        _site = site ?? throw new ArgumentNullException(nameof(site));
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
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
        Dictionary<string, int> counts = new(StringComparer.Ordinal);

        if (access.Has(OperationsPermissions.BrandEdit) && features.Includes(Vixly.Operations.Application.Plans.OperationsPlanModules.CustomerPortal))
        {
            counts["site"] = await _site.CountNewReviewsAsync(cancellationToken);
        }

        if (access.Has(OperationsPermissions.PaymentsConfirm))
        {
            IReadOnlyList<Vixly.Operations.Domain.Locations.OperationsWarehouse> warehouses = await _desk.WarehousesAsync(access, OperationsPermissions.PaymentsConfirm, cancellationToken);
            counts["verify"] = warehouses.Count == 0 ? 0 : await _payments.CountPendingAsync(warehouses.Select(warehouse => warehouse.Id).ToList(), cancellationToken);
        }

        return GetOperationsMenuResponse.Success(OperationsPermissions.All.Where(access.Has).ToHashSet(StringComparer.Ordinal), access.IsOwner, access.IsAdministrator,
            Vixly.Operations.Application.Plans.OperationsPlanModules.HiddenPages(features), !features.Includes(Vixly.Operations.Application.Plans.OperationsPlanModules.CustomerPortal),
            features.ModulesOff).WithCounts(counts);
    }
}
