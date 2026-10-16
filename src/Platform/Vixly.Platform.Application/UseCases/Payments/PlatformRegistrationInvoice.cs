using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Application.UseCases.Payments;

// =========================================================
// FILE: PlatformRegistrationInvoice.cs
//
// PURPOSE:
// Finds the invoice a self-registering company must pay.
//
// RESPONSIBILITIES:
// - Return the open first-period invoice of the tenant's subscription
//
// IMPORTANT NOTES:
// - Read only
// =========================================================
public static class PlatformRegistrationInvoice
{
    public static async Task<PlatformInvoice?> FindAsync(PlatformPaymentsAdministration payments, Guid tenantId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payments);

        Guid? invoiceId = await payments.Billing.Invoices.FindOpenFirstPeriodAsync(tenantId, cancellationToken);
        return invoiceId is Guid id ? await payments.Billing.Invoices.GetAsync(id, cancellationToken) : null;
    }
}
