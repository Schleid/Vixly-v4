using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.Queries;

namespace Vixly.Platform.Application.UseCases.Sales;

// =========================================================
// FILE: PlatformApprovalKinds.cs
//
// PURPOSE:
// The kinds of request a person may approve (four eyes).
//
// RESPONSIBILITIES:
// - Map each approval permission to the kinds it approves
//
// IMPORTANT NOTES:
// - Shared by the approvals list and the menu's count (owner, 2026-11-04)
// =========================================================
public static class PlatformApprovalKinds
{
    public static async Task<IReadOnlyList<PlatformApprovalKind>> ForAsync(PlatformSalesAdministration sales, PlatformAuthorizationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sales);
        ArgumentNullException.ThrowIfNull(context);

        List<PlatformApprovalKind> kinds = new();

        if (await sales.HoldsAsync(context, PlatformPermissions.DiscountsApprove, cancellationToken))
        {
            kinds.Add(PlatformApprovalKind.Discount);
            kinds.Add(PlatformApprovalKind.Offer);
        }

        if (await sales.HoldsAsync(context, PlatformPermissions.PromotionsApprove, cancellationToken))
        {
            kinds.Add(PlatformApprovalKind.Promotion);
        }

        if (await sales.HoldsAsync(context, PlatformPermissions.PlansPublish, cancellationToken))
        {
            kinds.Add(PlatformApprovalKind.PlanVersion);
        }

        if (await sales.HoldsAsync(context, PlatformPermissions.BillingApproveCredits, cancellationToken))
        {
            kinds.Add(PlatformApprovalKind.CreditNote);
        }

        return kinds;
    }
}
