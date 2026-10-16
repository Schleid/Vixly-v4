using Vixly.Platform.Application.Security;

namespace Vixly.Platform.Application.UseCases.Billing;

// =========================================================
// FILE: PlatformBillingAbilities.cs
//
// PURPOSE:
// Reads what the viewer may do in Billing.
//
// RESPONSIBILITIES:
// - Turn held permissions into abilities
//
// IMPORTANT NOTES:
// - Display only: each use case checks again
// =========================================================
public static class PlatformBillingAbilities
{
    public static async Task<PlatformInvoiceAbilities> ReadAsync(PlatformBillingAdministration billing, PlatformAuthorizationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(billing);

        return new PlatformInvoiceAbilities(
            await billing.HoldsAsync(context, PlatformPermissions.BillingRecordPayments, cancellationToken),
            await billing.HoldsAsync(context, PlatformPermissions.BillingRequestCredits, cancellationToken),
            await billing.HoldsAsync(context, PlatformPermissions.BillingApproveCredits, cancellationToken));
    }
}
